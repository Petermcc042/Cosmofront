using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

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

    public float AnimationFrame;
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

    public void RecalcPaths() 
    {
        // Start measuring time
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();

        // The code we want to time:
        PrecomputedData.RunFlowFieldJobs(99, 99, false);

        // Stop measuring time
        stopwatch.Stop();
        //Debug.Log($"Flow field recalculation took: {stopwatch.ElapsedMilliseconds} ms");
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
        float positionOnEdge = (index / (float)totalSpawns) * gridWidth; // Spread along edges

        int gridPosX = 0;
        int gridPosZ = 0;

        switch (edge)
        {
            case 0: // Top edge
                gridPosX = Mathf.FloorToInt(positionOnEdge);
                gridPosZ = gridLength - 1;
                break;
            case 1: // Right edge
                gridPosX = gridWidth - 1;
                gridPosZ = Mathf.FloorToInt(positionOnEdge);
                break;
            case 2: // Bottom edge
                gridPosX = Mathf.FloorToInt(positionOnEdge);
                gridPosZ = 0;
                break;
            case 3: // Left edge
                gridPosX = 0;
                gridPosZ = Mathf.FloorToInt(positionOnEdge);
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
            IsAtShield = false
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




     /// <summary>
     /// Old Debug Code to place an item where each enemy target position should be
     /// </summary>
     private void CheckPaths()
     {
        Debug.Log("Checking paths");


        // this is taking the first spawn position and getting the spawn position
        int currentIndexPostition = Mathf.FloorToInt(spawnOriginVectorList[0].z) + Mathf.FloorToInt(spawnOriginVectorList[0].x) * 200;
        Debug.Log(spawnOriginVectorList[0] + " : " + PrecomputedData.gridArray[currentIndexPostition].position);
        Instantiate(targetPosGO, PrecomputedData.gridArray[currentIndexPostition].position, Quaternion.identity, gameObject.transform);


        Debug.Log("go to index: " + PrecomputedData.gridArray[currentIndexPostition].goToIndex);
        Vector3 currentPos = PrecomputedData.gridArray[currentIndexPostition].position;
        Vector3 towardsCenter = (centre.position - currentPos).normalized;
        int newIndexPostition = Mathf.FloorToInt(currentPos.z + Mathf.Sign(towardsCenter.z)) +
                            Mathf.FloorToInt(currentPos.x + Mathf.Sign(towardsCenter.x)) * 200;
        Debug.Log("current position: " + currentPos + " - centre position: " + centre.position);
        Debug.Log("current index: " + currentIndexPostition + " - new index: " + newIndexPostition);
        Debug.Log("current index: " + PrecomputedData.gridArray[currentIndexPostition].position + " - new index: " + PrecomputedData.gridArray[newIndexPostition].position);
        Debug.Log("current index: " + PrecomputedData.gridArray[2000].position + " - new index: " + PrecomputedData.gridArray[4000].position);
        Instantiate(targetPosGO, PrecomputedData.gridArray[newIndexPostition].position, Quaternion.identity, gameObject.transform);
    }
}



