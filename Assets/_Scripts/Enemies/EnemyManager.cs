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
    public float3 Position; //Position = pathArray[pathIndexArray[_spawnIndexCount]]
    public float3 TargetPos;
    public float3 Velocity;
    public Quaternion Rotation;
    public float Speed; //Speed = 20
    public float Health; //EnemyHealth = 1
    public int Armour; //EnemyArmour = 1
    public int Damage;
    public bool ToRemove;
    public bool TargetNeeded;
    public bool IsActive;
    public bool IsAtShield;
    public float3 AttackPos;
}

public class EnemyManager : MonoBehaviour
{
    public event EventHandler<EnemyDamageEventArgs> EnemyDamaged;
    public class EnemyDamageEventArgs : EventArgs { public int enemyObjectID; public int damagedAmount; }

    public event EventHandler<EnemyDestroyedEventArgs> EnemyDestroyed;
    public class EnemyDestroyedEventArgs : EventArgs { public int enemyObjectID; }

    [SerializeField] private bool showDebugLines;
    [SerializeField] private MapGridManager mapGridManager;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private WallManager wallManager;
    [SerializeField] private SkillManager skillManager;
    [SerializeField] private CollisionManager collisionManager;
    [SerializeField] private Generator gen;
    [SerializeField] private GameObject generator;
    [SerializeField] private NewPathfinding pathfinding;


    [Header("Gameplay Variables")]
    [SerializeField] private string csvPathString;
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
    //private NativeArray<Vector3> pathArray;
    //private NativeArray<int> pathIndexArray;

    private int activeEnemyCount;

    // lists of whether to logic to apply to enemies
    public NativeList<EnemyData> enemyDataList;
    private List<GameObject> enemyList;
    private List<GameObject> damageEnemyList;

    private List<GameObject> enemyTargetList;
    [SerializeField] GameObject targetPosGO;


    // For the enemy entity
    [Header("Enemy Variables")]
    [SerializeField] GameObject enemyParent;
    [SerializeField] GameObject enemyVisual;
    [SerializeField] GameObject bossVisual;

    [SerializeField] private int speed;

    public Transform centre;


    void Awake()
    {

        skillManager.OnSkillUnlocked += SkillManager_OnSkillUnlocked;

        enemyList = new List<GameObject>();
        enemyTargetList = new List<GameObject>();
        enemyDataList = new NativeList<EnemyData>(Allocator.Persistent);

        damageEnemyList = new List<GameObject>();
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

    private void OnDestroy()
    {
        skillManager.OnSkillUnlocked -= SkillManager_OnSkillUnlocked;
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
        pathfinding.StartFlowField(centre.position, true); // create the flow field

        gen.CheckShieldSquares(false);

        //CheckPaths();


        // increase timer to allow spawn loop to begin, then begin spawning
        spawnInterval = spawnCountdown;
        //keepSpawning = true; 
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

    public void UpdateEnemyPositions(NativeList<int> indexToRemove, NativeList<EnemyData> enemyDataList)
    {
        for (int i = 0; i < indexToRemove.Length; i++)
        {
            Destroy(enemyList[indexToRemove[i]]);
            enemyList.RemoveAt(indexToRemove[i]);
            enemyCount--;

            Destroy(enemyTargetList[indexToRemove[i]]);
            enemyTargetList.RemoveAt(indexToRemove[i]);
        }

        // finally updating the game object positions as can't in jobs
        for (int i = 0; i < enemyList.Count; i++)
        {
            enemyList[i].transform.position = enemyDataList[i].Position;// + new Vector3(0.5f, 0, 0.5f);
            enemyList[i].transform.rotation = enemyDataList[i].Rotation;
            enemyTargetList[i].transform.position = enemyDataList[i].TargetPos;
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
        pathfinding.RecalcFlowField(centre.position, false); // create the flow field

        // Stop measuring time
        stopwatch.Stop();
        Debug.Log($"Flow field recalculation took: {stopwatch.ElapsedMilliseconds} ms");
    }

    private void SetSpawnPositions(int numSpawnPositions)
    {
        long totalTime = 0;

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

        // select a random number to refer to a random enemy instantiate that enemy under the parent
        int randomValue = Mathf.RoundToInt(UnityEngine.Random.Range(0, enemyWeightSum + 1));
        int enemyIndex = FindIndexInCumulativeSum(enemyWeightList, randomValue);
        Instantiate(gameSettingsSO.enemyPrefabList[enemyIndex], spawnPoint, Quaternion.identity, enemy.transform);

        enemyList.Add(enemy);

        GameObject targetPos = Instantiate(targetPosGO, spawnPoint, Quaternion.identity, enemy.transform);
        enemyTargetList.Add(targetPos);

        int tempDamage = (gameManager.invincible) ? 0 : 2;


        EnemyData eData = new()
        {
            EnemyID = enemyCount,
            Health = gameSettingsSO.enemyHealthList[enemyIndex],
            Armour = 1,
            Damage = tempDamage,
            Position = spawnPoint,// take the overall list and based on the 
            Speed = 8,
            Velocity = Vector3.zero,
            ToRemove = false,
            TargetPos = Vector3.zero,
            TargetNeeded = true,
            IsActive = true
        };

        //collisionManager.AddEnemyData(eData);
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

    private void SkillManager_OnSkillUnlocked(object sender, SkillManager.OnSkillUnlockedEventArgs e)
    {
        switch (e.skillType)
        {
            case SkillManager.SkillType.SpikedWalls:
                break;
        }
    }




    /// <summary>
    /// Old Debug Code to place an item where each enemy target position should be
    /// </summary>
    private void CheckPaths()
    {
        Debug.Log("Checking paths");

        int currentIndexPostition = Mathf.FloorToInt(spawnOriginVectorList[0].z) + Mathf.FloorToInt(spawnOriginVectorList[0].x) * 200;
        Debug.Log(spawnOriginVectorList[0] + " : " + pathfinding.flowNodes[currentIndexPostition].position);

        for (int i = 0; i < 150; i++)
        {
            Instantiate(targetPosGO, pathfinding.flowNodes[currentIndexPostition].position, Quaternion.identity, gameObject.transform);
            if (pathfinding.flowNodes[currentIndexPostition].goToIndex < 0)
            {
                // Move one square towards center if we hit a negative index
                Vector3 currentPos = pathfinding.flowNodes[currentIndexPostition].position;
                Vector3 towardsCenter = (centre.position - currentPos).normalized;
                currentIndexPostition = Mathf.FloorToInt(currentPos.z + Mathf.Sign(towardsCenter.z)) +
                                      Mathf.FloorToInt(currentPos.x + Mathf.Sign(towardsCenter.x)) * 200;
            }
            else
            {
                currentIndexPostition = pathfinding.flowNodes[currentIndexPostition].goToIndex;
            }

        }
    }



    // OLD CODE
    public void DestroyEnemy(GameObject _enemy)
    {
        if (_enemy.GetComponent<GhostEnemy>() == null)
        {
            //InstantiateSpecificEnemyObject(_enemy.transform.position, )
        }

        EnemyDestroyed?.Invoke(this, new EnemyDestroyedEventArgs { enemyObjectID = _enemy.GetInstanceID() });

        if (enemyList.Contains(_enemy)) { enemyList.Remove(_enemy); }
        if (damageEnemyList.Contains(_enemy)) { damageEnemyList.Remove(_enemy); }
        _enemy.tag = "Untagged";
        _enemy.layer = 0;
        _enemy.GetComponentInChildren<Animator>().Play("Die");

        //_enemy.transform.position = Vector3.zero;
        //_enemy.GetComponent<Enemy>().ResetEnemy();
        //repurposeEnemyList.Add(_enemy);
        StartCoroutine(DestroyGO(_enemy));
        enemyCount--;
    }

    IEnumerator DestroyGO(GameObject _enemy)
    {
        yield return new WaitForSeconds(1f);
        Destroy(_enemy);
    }

    public void CallWinGame()
    {
        gameManager.EndGame("You Win");
    }



}



