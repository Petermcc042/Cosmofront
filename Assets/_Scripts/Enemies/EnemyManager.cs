using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

public struct EnemyData
{
    public int EnemyID;
    public float3 Position;
    public float3 TargetPos;
    public float3 Velocity;
    public Quaternion Rotation;
    public float Speed; //Speed = 20
    public float Health; //EnemyHealth = 1
    public int Armour; //EnemyArmour = 1
    public int Damage;
    public bool IsDead;
    public bool TargetNeeded;
    public bool IsAttacking; // swtich bool
    public bool IsAtShield;
    public float3 AttackPos;
    public uint enemyType;
    public float AnimationFrame;
    public int hitCount;
}


public class EnemyManager : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private SkillManager skillManager;
    [SerializeField] private Generator gen;
    [SerializeField] private GameObject generator;


    [Header("Gameplay Variables")]
    [SerializeField] GameSettingsSO gameSettingsSO;
    public int enemyCount;

    [Header("Phase Variables")]
    [SerializeField] public List<int> weightsOne;
    [SerializeField] public List<int> weightsTwo;
    [SerializeField] public List<int> weightsThree;
    [SerializeField] public List<int> weightsFour;
    [SerializeField] public List<int> weightsFive;
    [SerializeField] public List<List<int>> enemyWeights;
    private List<int> enemyWeightList;

    //private int enemyPrefabIndex = 1;

    private int numberOfSpawns;
    private int gridWidth;
    private int gridLength;

    // for phase intervals
    private int spawnPhase = 0;

    // Variables for spawning
    private float spawnCountdown = 1;
    public bool keepSpawning = false;
    private float spawnInterval = 1;
    private int spawnIndexCount;
    private int enemyWeightSum;

    // pathfinding lists
    private List<Vector3> spawnOriginVectorList;
    public List<List<Vector3>> pathLists;

    // lists of whether to logic to apply to enemies
    public NativeList<EnemyData> enemyDataList;
    private List<GameObject> enemyList;

    private List<GameObject> enemyTargetList;
    [SerializeField] GameObject targetPosGO;


    // For the enemy entity
    [Header("Enemy Variables")]
    [SerializeField] GameObject enemyParent;
    public Transform centre;


    void Awake()
    {
        enemyList = new List<GameObject>();
        enemyTargetList = new List<GameObject>();
        enemyDataList = new NativeList<EnemyData>(Allocator.Persistent);

        spawnOriginVectorList = new List<Vector3>();
        pathLists = new List<List<Vector3>>();

        enemyWeights = new List<List<int>>
        {
            weightsOne,
            weightsTwo,
            weightsThree,
            weightsFour,
            weightsFive,
        };
    }

    public void InitEnemyManager(GameSettingsSO _gameSettings)
    {
        // use game settings file in inspector to dictate most game attributes
        gameSettingsSO = _gameSettings;

        numberOfSpawns = _gameSettings.numberOfSpawns;
        gridWidth = _gameSettings.gridWidth;
        gridLength = _gameSettings.gridLength;
        enemyWeightList = _gameSettings.enemyWeightList;
        enemyWeightSum = enemyWeightList.Sum();


        centre = generator.transform; // set the target position for all the enemy pathfinding

        SetSpawnPositions(numberOfSpawns); // set the spawn positions

        gen.CheckShieldSquares(false);

        // leave for debug testing 
        //CheckPaths();

        // increase timer to allow spawn loop to begin, then begin spawning
        spawnInterval = spawnCountdown;
    }


    public void CallUpdate()
    {
        float deltaTime = Time.deltaTime;

        if (keepSpawning)
        {
            if (spawnCountdown <= 0f)
            {
                SpawnIndividualObjects();
                spawnCountdown = spawnInterval;
            }

            spawnCountdown -= deltaTime;
        }
    }

    public void UpdateEnemyPositions(NativeList<int> indexToRemove)
    {
        //Debug.Log($"Enemy Objects: {enemyList.Count} - enemy data: {enemyDataList.Length}");
        for (int i = 0; i < indexToRemove.Length; i++)
        {
            Destroy(enemyList[indexToRemove[i]]);
            enemyList.RemoveAt(indexToRemove[i]);
            enemyCount--;

            //Destroy(enemyTargetList[indexToRemove[i]]);
            //enemyTargetList.RemoveAt(indexToRemove[i]);
        }

        // finally updating the game object positions as can't in jobs
        for (int i = 0; i < enemyList.Count; i++)
        {
            enemyList[i].transform.position = enemyDataList[i].Position;// + new Vector3(0.5f, 0, 0.5f);
            enemyList[i].transform.rotation = enemyDataList[i].Rotation;
            //enemyTargetList[i].transform.position = enemyDataList[i].TargetPos;
        }
    }

    public void updatePhase(int _indexPos)
    {
        spawnPhase = _indexPos;
        keepSpawning = true;
        spawnInterval = gameSettingsSO.spawnIntervalList[_indexPos];
        enemyWeightList = enemyWeights[_indexPos];
        enemyWeightSum = enemyWeightList.Sum();
    }

    private void SetSpawnPositions(int numSpawnPositions)
    {
        for (int i = 0; i < numSpawnPositions; i++)
        {
            Vector3 spawnPos = GetSpawnPosition(i, numSpawnPositions);
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = spawnPos;
            spawnOriginVectorList.Add(spawnPos);
        }
    }

    private Vector3 GetSpawnPosition(int index, int totalSpawns)
    {
        int edge = index % 4; // 4 edges: top, right, bottom, left
        float positionOnEdge = (index / (float)totalSpawns) * (gridWidth - 4); // Adjust for 2-tile inset on both sides

        int gridPosX = 0;
        int gridPosZ = 0;

        switch (edge)
        {
            case 0: // Top edge (moving 2 tiles inward)
                gridPosX = 2 + Mathf.FloorToInt(positionOnEdge);
                gridPosZ = gridLength - 3;
                break;
            case 1: // Right edge
                gridPosX = gridWidth - 3;
                gridPosZ = 2 + Mathf.FloorToInt(positionOnEdge);
                break;
            case 2: // Bottom edge
                gridPosX = 2 + Mathf.FloorToInt(positionOnEdge);
                gridPosZ = 2;
                break;
            case 3: // Left edge
                gridPosX = 2;
                gridPosZ = 2 + Mathf.FloorToInt(positionOnEdge);
                break;
        }

        return new Vector3(gridPosX, 0, gridPosZ);
    }


    private void SpawnIndividualObjects()
    {
        if (spawnIndexCount >= numberOfSpawns) { spawnIndexCount = 0; } // spawn index count never exceeds the number of paths to the centre

        Vector3 spawnPoint = spawnOriginVectorList[spawnIndexCount];

        GameObject enemy = Instantiate(enemyParent, spawnPoint, Quaternion.identity, gameObject.transform);

        int randomValue = Mathf.RoundToInt(UnityEngine.Random.Range(0, enemyWeightSum + 1));
        int enemyIndex = FindIndexInCumulativeSum(enemyWeightList, randomValue);

        // select a random number to refer to a random enemy instantiate that enemy under the parent
        if (gameManager.renderGameObjects)
        {   
            Instantiate(gameSettingsSO.enemyPrefabList[enemyIndex], spawnPoint, gameSettingsSO.enemyPrefabList[enemyIndex].transform.rotation, enemy.transform);
        }
        

        enemyList.Add(enemy);

        
        // DEBUG:can be used to show target position of enemy
        //GameObject targetPos = Instantiate(targetPosGO, spawnPoint, Quaternion.identity, enemy.transform);
        //enemyTargetList.Add(targetPos);

        int tempDamage = (gameManager.invincible) ? 0 : 2;


        EnemyData eData = new()
        {
            EnemyID = enemyCount,
            Health = gameSettingsSO.enemyHealthList[enemyIndex],
            Armour = 1,
            Damage = tempDamage,
            Position = spawnPoint,
            Speed = 8,
            Velocity = float3.zero,
            IsDead = false,
            TargetPos = float3.zero,
            TargetNeeded = true,
            IsAttacking = false,
            IsAtShield = false,
            enemyType = (uint)UnityEngine.Random.Range(1, 4)
        };

        enemyDataList.Add(eData);

        spawnIndexCount++;
        enemyCount++;
    }


    static int FindIndexInCumulativeSum(List<int> list, int randomInteger)
    {
        int cumulativeSum = 0;

        for (int i = 0; i < list.Count; i++)
        {
            cumulativeSum += list[i];

            if (cumulativeSum >= randomInteger)
            {
                return i;
            }
        }

        // If the random integer is greater than the sum of all elements, return the last index
        return list.Count - 1;
    }


    public List<GameObject> ReturnEnemyObjectList()
    {
        return enemyList; 
    }

    #region Debug

    private static readonly int[] NeighborOffsetsX = { -1, 0, 1, -1, 1, -1, 0, 1 };
    private static readonly int[] NeighborOffsetsZ = { -1, -1, -1, 0, 0, 1, 1, 1 };

    public void CheckPaths()
    {
        List<Vector3> pathPositions = new List<Vector3>();
        Vector3 currentPos = spawnOriginVectorList[30];

        // Find neighbor with lowest integration cost
        int bestNeighborIndex = -1;
        float lowestCost = float.MaxValue;

        for (int j =0; j < 150; j++)
        {
            int gridX = (int)math.floor(currentPos.x);
            int gridZ = (int)math.floor(currentPos.z);
            // Check all neighbors
            for (int i = 0; i < 8; i++)
            {
                int neighborX = gridX + NeighborOffsetsX[i];
                int neighborZ = gridZ + NeighborOffsetsZ[i];

                // Get the index in the grid array
                int neighborIndex = neighborZ + neighborX * 200;

                // Make sure the neighbor index is valid
                if (neighborIndex >= 0 && neighborIndex < PrecomputedData.gridArray.Length)
                {
                    float neighborCost = PrecomputedData.gridArray[neighborIndex].integrationCost;

                    // Find the neighbor with the lowest cost
                    if (neighborCost < lowestCost)
                    {
                        lowestCost = neighborCost;
                        bestNeighborIndex = neighborIndex;
                    }
                }
            }

            if (bestNeighborIndex != -1)
            {
                pathPositions.Add(PrecomputedData.gridArray[bestNeighborIndex].position);
            }

            currentPos = PrecomputedData.gridArray[bestNeighborIndex].position;
            pathPositions.Add(currentPos);
        }

        instanceCount = pathPositions.Count;

        matrixList = new List<Matrix4x4>(instanceCount); // Preallocate

        // Assign initial positions
        for (int i = 0; i < instanceCount; i++)
        {
            float3 position = pathPositions[i];

            Quaternion rotation = Quaternion.identity;
            Vector3 scale = Vector3.one;

            matrixList.Add(Matrix4x4.TRS(position, rotation, scale)); // Prepopulate list
        }

        material.enableInstancing = true;
        isCreated = true;
    }

    public Mesh mesh;
    public Material material;
    private int instanceCount = 100;

    private List<Matrix4x4> matrixList;
    private bool isCreated = false;

    private void Update()
    {
        if (!isCreated) { return; }
        Graphics.DrawMeshInstanced(mesh, 0, material, matrixList);
    }

    #endregion
}




