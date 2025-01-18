using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

public struct EnemyData
{
    public int EnemyID;
    public int PathIndex; //PathIndex = _spawnIndexCount can be 0 through to _numberOfSpawns
    public int MaxPathIndex;
    public int PathPositionIndex; //PositionIndex = 0 increments as the enemy moves along the apth
    public Vector3 Position; //Position = pathArray[pathIndexArray[_spawnIndexCount]]
    public Vector3 TargetPos;
    public Vector3 Velocity;
    public Quaternion Rotation;
    public float Speed; //Speed = 20
    public float Health; //EnemyHealth = 1
    public int Armour; //EnemyArmour = 1
    public int Damage;
    public bool ToRemove;
    public bool TargetNeeded;
    public bool IsActive;
    public bool IsAtShield;
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

    private Transform centre;

    void Awake()
    {
        skillManager.OnSkillUnlocked += SkillManager_OnSkillUnlocked;

        enemyList = new List<GameObject>();
        enemyTargetList = new List<GameObject>();

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
        gameSettingsSO = _gameSettings;

        numberOfSpawns = _gameSettings.numberOfSpawns;
        gridWidth = _gameSettings.gridWidth;
        gridLength = _gameSettings.gridLength;

        enemyWeightList = _gameSettings.enemyWeightList;
        enemyWeightSum = enemyWeightList.Sum();

        spawnInterval = spawnCountdown;

        centre = generator.transform;

        SetSpawnPositions(numberOfSpawns);

        keepSpawning = false;

        gen.CheckShieldSquares(false);

        keepSpawning = true;
    }



    private void Update()
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
        }

        // finally updating the game object positions as can't in jobs
        for (int i = 0; i < enemyList.Count; i++)
        {
            enemyList[i].transform.position = enemyDataList[i].Position;// + new Vector3(0.5f, 0, 0.5f);
            enemyList[i].transform.rotation = enemyDataList[i].Rotation;
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

    IEnumerator PauseSpawning()
    {
        Debug.Log("pausing spawning");
        yield return new WaitForSeconds(5f);
    }

    private void SetSpawnPositions(int numSpawnPositions)
    {
        long totalTime = 0;

        for (int i = 0; i < numSpawnPositions; i++)
        {
            Vector3 spawnPos = GetSpawnPosition(i, numSpawnPositions);

            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            List<Vector3> tempList = SetTargetPosition(spawnPos, centre.position);

            stopwatch.Stop();
            Debug.Log($"Time taken: {stopwatch.ElapsedMilliseconds} ms");
            totalTime += stopwatch.ElapsedMilliseconds;

            if (tempList == null)
            {
                i--;
                continue;
            }

            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = spawnPos;

            pathLists.Add(tempList);
            spawnOriginVectorList.Add(spawnPos);
        }


        Debug.Log($"Time taken: {totalTime} ms");

        collisionManager.CombinedPaths(pathLists);
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



    // Get the pathfinding from base to centre
    private List<Vector3> SetTargetPosition(Vector3 _spawnPos, Vector3 _targetPosition)
    {
        List<Vector3> pathList = Pathfinding.Instance.FindPath(_spawnPos, _targetPosition);
        //List<Vector3> pathList = pathfinding.FindPath(_targetPosition, _spawnPos);

        if (showDebugLines)
        {
            if (pathList != null)
            {
                for (int i = 0; i < pathList.Count-1; i++)
                {
                    Debug.DrawLine(new Vector3(pathList[i].x, 1, pathList[i].z) + Vector3.one * 0.5f, new Vector3(pathList[i + 1].x, 1, pathList[i + 1].z) + Vector3.one * 0.5f, UnityEngine.Color.black, 100f);
                }
            }
        }

        return pathList;
    }

    public void RecalcPaths() {
        long totalTime = 0;

        for (int i = 0; i < numberOfSpawns; i++)
        {
            Vector3 spawnPos = GetSpawnPosition(i, numberOfSpawns);

            //System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            //stopwatch.Start();

            List<Vector3> tempList = SetTargetPosition(spawnPos, centre.position);

            //stopwatch.Stop();
            //Debug.Log($"Time taken: {stopwatch.ElapsedMilliseconds} ms");
            //totalTime += stopwatch.ElapsedMilliseconds;

            if (tempList == null)
            {
                i--;
                continue;
            }

            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //cube.GetComponent<MeshRenderer>().
            cube.transform.position = spawnPos;
        }
    }

    private void SpawnIndividualObjects()
    {
        if (pathLists.Count == 0) { return; }

        if (spawnIndexCount >= pathLists.Count) { spawnIndexCount = 0; } // spawn index count never exceeds the number of paths to the centre

        InstantiateEnemyObject(collisionManager.pathArray[collisionManager.pathIndexArray[spawnIndexCount]], spawnIndexCount);
        enemyCount++;
        spawnIndexCount++;
    }

    private void InstantiateEnemyObject(Vector3 _spawnPoint, int _index)
    {
        int tempInt;

        if (_index == numberOfSpawns-1)
            tempInt = collisionManager.pathArray.Length;
        else
            tempInt = collisionManager.pathIndexArray[_index + 1];

        GameObject enemy = Instantiate(enemyParent, _spawnPoint, Quaternion.identity, gameObject.transform);

        // select a random number to refer to a random enemy instantiate that enemy under the parent
        int randomValue = Mathf.RoundToInt(UnityEngine.Random.Range(0, enemyWeightSum + 1));
        int enemyIndex = FindIndexInCumulativeSum(enemyWeightList, randomValue);
        Instantiate(gameSettingsSO.enemyPrefabList[enemyIndex], _spawnPoint, Quaternion.identity, enemy.transform);

        enemyList.Add(enemy);

        //GameObject targetPos = Instantiate(targetPosGO, _spawnPoint, Quaternion.identity, enemy.transform);
        //enemyTargetList.Add(targetPos);

        EnemyData eData = new()
        {
            EnemyID = enemyCount,
            Health = gameSettingsSO.enemyHealthList[enemyIndex],
            Armour = 1,
            Damage = 1,
            Position = collisionManager.pathArray[collisionManager.pathIndexArray[_index]],// take the overall list and based on the 
            PathPositionIndex = 1,
            PathIndex = collisionManager.pathIndexArray[_index],
            MaxPathIndex = tempInt, // can be past 
            Speed = 10,
            Velocity = Vector3.zero,
            ToRemove = false,
            TargetPos = Vector3.zero,
            TargetNeeded = true,
            IsActive = true
        };

        collisionManager.AddEnemyData(eData);
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



