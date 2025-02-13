using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Xml;
using TMPro;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.EventSystems.EventTrigger;

public class CollisionManager : MonoBehaviour
{
    public event EventHandler<TurretXPEventArgs> TurretXPEvent;
    public class TurretXPEventArgs : EventArgs { public int turretID; public int xpAmount; }

    [SerializeField] public EnemyManager enemyManager;
    [SerializeField] public BulletManager bulletManager;
    [SerializeField] public MapGridManager mapGridManager;
    [SerializeField] public GameManager gameManager;
    [SerializeField] public Generator gen;
    [SerializeField] public TerrainGen terrainGen;
    [SerializeField] public ParticlePool particlePool;
    [SerializeField] public NewPathfinding pathfinding;

    [SerializeField] private Slider genHealth;
    [SerializeField] private int explosionRadius;
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private GameObject lightningPrefab;
    [SerializeField] private Material lightningPrefabMaterial;

    //public NativeArray<Vector3> pathArray;
    //public NativeArray<int> pathIndexArray;
    private NativeList<Vector3> obstructPathList;
    private NativeList<Vector3> shieldPositions;

    private NativeList<EnemyData> enemyDataList;
    private NativeList<BulletData> bulletDataList;
    private NativeArray<Vector3> terrainDataArray;

    private int enemyXPAmount = 0;

    public float shieldDamageAmount;

    public GameObject damageTextPrefab;

    uint mySeed = 1;

    private List<(GameObject obj, float timer)> lightningVFX = new List<(GameObject obj, float timer)>();
    private const float LIGHTNING_DURATION = 0.1f;

    void Awake()
    {
        enemyDataList = new NativeList<EnemyData>(Allocator.Persistent);
        bulletDataList = new NativeList<BulletData>(Allocator.Persistent);
        terrainDataArray = new NativeArray<Vector3>(0, Allocator.Persistent);
    }

    private void Start()
    {
        obstructPathList = mapGridManager.buildingGridSquareList;
        shieldPositions = gen.shieldGridSquareList; 
    }

    private void OnDestroy()
    {
        enemyDataList.Dispose();
        bulletDataList.Dispose();
        terrainDataArray.Dispose();
    }

    public void InitCollisionManager(GameSettingsSO _gameSettings)
    {
        enemyXPAmount = _gameSettings.enemyXPList[0];
    }

    public void CreateTerrainArray(List<Vector3> _blockedCoords)
    {
        Debug.Log("reduce terrain data coords for collision from " + _blockedCoords.Count);
        terrainDataArray = new(_blockedCoords.Count, Allocator.Persistent);

        for (int i = 0; i < terrainDataArray.Length; i++)
        {
            terrainDataArray[i] = _blockedCoords[i];
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P)) { gameManager.InvertGameState(); }

        if (!gameManager.GetGameState())
        {
            float deltaTime = Time.deltaTime;
            UpdatePositions(deltaTime);
            CheckBulletCollisions();
            RemovalAndUpdate();
            DamageBuildings();
            UpdateLightningVFX();
        }
    }

    private void UpdatePositions(float _deltaTime)
    {
        uint seed = (mySeed != 0) ? mySeed : 1;

        NativeArray<EnemyData> enemyDataOffset = new NativeArray<EnemyData>(enemyDataList.AsArray().Length, Allocator.Persistent);

        for (int i = 0; i < enemyDataList.AsArray().Length; i++) {
            enemyDataOffset[i] = enemyDataList.AsArray()[i];
        }

        var updateEnemyTargetJob = new UpdateEnemyTargetPos
        {
            EnemyData = enemyDataList.AsArray(),
            EnemyDataOffset = enemyDataOffset,
            FlowGridArray = pathfinding.flowNodes,
            Seed = seed
        };

        JobHandle updateEnemyTargetPosHandle = updateEnemyTargetJob.Schedule(enemyDataList.Length, 64);
        updateEnemyTargetPosHandle.Complete();
        
        enemyDataOffset.Dispose();

        var moveEnemyJob = new UpdateEnemyPosition
        {
            EnemyData = enemyDataList.AsArray(),
            ShieldPositions = shieldPositions.AsArray(),
            ObstructedPositions = obstructPathList.AsArray(),
            DeltaTime = _deltaTime
        };

        JobHandle moveEnemyHandle = moveEnemyJob.Schedule(enemyDataList.Length, 64);


        var bulletMoveJob = new UpdateBulletPosition
        {
            BulletList = bulletDataList.AsArray(),
            DeltaTime = _deltaTime
        };

        JobHandle bulletMoveHandle = bulletMoveJob.Schedule(bulletDataList.Length, 64);

        JobHandle.CompleteAll(ref bulletMoveHandle, ref moveEnemyHandle);
    }


    private void CheckBulletCollisions()
    {
        NativeQueue<CollisionData> colData = new(Allocator.Persistent);
        NativeQueue<LightningVFXData> lightningData = new(Allocator.Persistent);
        NativeQueue<int> enemySlowData = new(Allocator.Persistent);
        NativeQueue<float3> impactPositions = new(Allocator.Persistent);
        NativeQueue<BulletData> createBulletData = new(Allocator.Persistent);

        var bulletCollisionJob = new BulletCollision
        {
            EnemyData = enemyDataList.AsArray(),
            TerrainData = terrainDataArray,
            ExplosionRadius = explosionRadius,
            ChainRadius = 10,
            MaxChainTargets = 10,
            ChainDamageReductionFactor = 0.8f,
            BulletData = bulletDataList.AsArray(),
            CollisionQueue = colData.AsParallelWriter(),
            VFXQueue = lightningData.AsParallelWriter(),
            EnemyIndexSlow = enemySlowData.AsParallelWriter(),
            ImpactPositions = impactPositions.AsParallelWriter(),
            CreateBulletData = createBulletData.AsParallelWriter()
        };

        JobHandle bulletHandle = bulletCollisionJob.Schedule(bulletDataList.Length, 64);
        bulletHandle.Complete();


        NativeList<int> turretXP = new(Allocator.Persistent);

        var collisionJob = new EnemyCollisionData
        {
            EnemyDataArray = enemyDataList.AsArray(),
            CollisionQueue = colData,
            TurretIDList = turretXP,
        };

        JobHandle collisionJobHandle = collisionJob.Schedule(bulletHandle);
        collisionJobHandle.Complete();


        float3 previousPosition = new float3(1.0f, 2.0f, 3.0f);

        while (impactPositions.TryDequeue(out float3 position))
        {
            if (math.any(position != previousPosition))
            {
                GameObject explosionInstance = Instantiate(explosionPrefab, position, Quaternion.identity);

                // Scale the explosion to a radius of 4
                explosionInstance.transform.localScale = new Vector3(4, 4, 4);

                // Optionally, destroy the explosion object after a certain time (if it's just a visual effect)
                Destroy(explosionInstance, 0.5f);

                previousPosition = position;
            }
        }

        while (lightningData.TryDequeue(out LightningVFXData data))
        {
            Vector3 start = data.StartPos;
            Vector3 end = data.EndPos;

            Vector3 midpoint = (start + end) / 2.0f;
            float distance = Vector3.Distance(start, end);

            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.GetComponent<MeshRenderer>().material = lightningPrefabMaterial;
            cube.transform.position = midpoint;
            Vector3 scale = new Vector3(0.1f, 0.1f, distance);
            cube.transform.localScale = scale;
            cube.transform.rotation = Quaternion.LookRotation(end - start);

            // Add to tracking list instead of using Destroy
            lightningVFX.Add((cube, 0f));
        }

        while (enemySlowData.TryDequeue(out int _index))
        {
            EnemyData eData = enemyDataList[_index];
            eData.Speed = 2;
            enemyDataList[_index] = eData;
        }

        while (createBulletData.TryDequeue(out BulletData _data))
        {
            if (_data.Type == BulletType.Spread)
            {
                Vector3 originalDirection = _data.Velocity.normalized;

                for (int i = 0; i < 3; i++)
                {
                    float spreadAngle = (i - 1) * 0.5f; // Adjust spread factor as needed
                    Vector3 spreadDirection = Quaternion.AngleAxis(spreadAngle * 30f, Vector3.up) * originalDirection; // Rotate around Y axis for fan-out

                    bulletManager.SpawnBullet(_data.Position, spreadDirection * _data.Velocity.magnitude, 5, _data.TurretID, _data.Damage, 0, BulletType.Standard, BulletType.Blank);
                }
            }
            else if (_data.Type == BulletType.FirestormPayload)
            {
                bulletManager.SpawnBullet(_data.Position, Vector3.zero, 0, _data.TurretID, 1, 0, BulletType.Firestorm, BulletType.Blank);
            }
            else if (_data.Type == BulletType.Slow)
            {
                bulletManager.SpawnBullet(_data.Position, Vector3.zero, 0, _data.TurretID, 1, 0, BulletType.Timewarp, BulletType.Blank);
            }
            else
            {
                bulletManager.SpawnBullet(_data.Position, Vector3.zero, 0, _data.TurretID, 1, 0, BulletType.Standard, BulletType.Blank);
            }
        }


        for (int i = 0; i < turretXP.Length; i++)
        {
            TurretXPEvent?.Invoke(this,
                new TurretXPEventArgs
                {
                    turretID = turretXP[i],
                    xpAmount = enemyXPAmount
                });
        }

        turretXP.Dispose();
        colData.Dispose();
        impactPositions.Dispose();
        lightningData.Dispose();
        enemySlowData.Dispose();
        createBulletData.Dispose();
    }

    private void UpdateLightningVFX()
    {
        if (gameManager.GetGameState()) return; // Skip if game is paused

        for (int i = lightningVFX.Count - 1; i >= 0; i--)
        {
            var (obj, timer) = lightningVFX[i];
            timer += Time.deltaTime;

            if (timer >= LIGHTNING_DURATION)
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
                lightningVFX.RemoveAt(i);
            }
            else
            {
                lightningVFX[i] = (obj, timer);
            }
        }
    }

    private void RemovalAndUpdate()
    {
        // create index to remove list
        // create job to find enemy to remove
        // finish job and send list to enemy manager
        NativeList<int> enemyToRemove = new NativeList<int>(Allocator.Persistent);
        NativeList<int> bulletToRemove = new NativeList<int>(Allocator.Persistent);

        var checkActiveEnemyJob = new CheckActiveEnemies
        {
            EnemyData = enemyDataList,
            IndexToRemove = enemyToRemove
        };

        JobHandle checkActiveEnemyJobHandle = checkActiveEnemyJob.Schedule();

        var checkActiveBulletJob = new CheckActiveBullets
        {
            BulletData = bulletDataList,
            IndexToRemove = bulletToRemove
        };

        JobHandle checkActiveBulletJobHandle = checkActiveBulletJob.Schedule();
        JobHandle.CompleteAll(ref checkActiveEnemyJobHandle, ref checkActiveBulletJobHandle);

        enemyManager.UpdateEnemyPositions(enemyToRemove, enemyDataList);
        bulletManager.UpdateBulletData(bulletToRemove, bulletDataList);

        enemyToRemove.Dispose();
        bulletToRemove.Dispose();
    }


    private void DamageBuildings()
    {
        NativeArray<BuildingCollisionData> buildingCollisionDataArray = new NativeArray<BuildingCollisionData>(enemyDataList.Length, Allocator.Persistent);

        var buildingCollisionJob = new BuildingCollision
        {
            EnemyDataArray = enemyDataList.AsArray(),
            BuildingCollisionDataArray = buildingCollisionDataArray,
            DeltaTime = Time.deltaTime
        };

        JobHandle buildingCollisionHandle = buildingCollisionJob.Schedule(enemyDataList.Length, 64);

        NativeArray<float> _tempShieldDamage = new NativeArray<float>(1, Allocator.TempJob);

        var shieldCollisionJob = new ShieldCollisionJob
        {
            EnemyDataArray = enemyDataList.AsArray(),
            ShieldDamageAmount = _tempShieldDamage,
            DeltaTime = Time.deltaTime
        };

        var shieldCollisionHandle = shieldCollisionJob.Schedule();

        JobHandle.CompleteAll(ref buildingCollisionHandle, ref shieldCollisionHandle);


        for (int i = 0; i < buildingCollisionDataArray.Length; i++)
        {
            BuildingCollisionData tempData = buildingCollisionDataArray[i];
            PlacedObject tempObject = mapGridManager.mapGrid.GetGridObject(tempData.GridPosition).GetPlacedObject();
            if (tempObject != null)
            {
                tempObject.DealDamage(tempData.Damage);
                if (tempObject.visibleName == "Generator")
                {
                    genHealth.value = tempObject.health / 100;
                }

                if (tempObject.health <= 0)
                {
                    if (tempObject.visibleName == "Generator")
                    {
                        genHealth.value = tempObject.health/100;
                        gameManager.EndGame("You Lose!");
                        break;
                    }

                    mapGridManager.mapGrid.GetXZ(tempData.GridPosition, out int _x, out int _z);
                    mapGridManager.DestroyBuilding(_x, _z);
                }
            }
        }

        buildingCollisionDataArray.Dispose();

        // Retrieve the calculated shield damage
        gen.DamageLoop(_tempShieldDamage[0], Time.deltaTime);

        _tempShieldDamage.Dispose();  // Dispose of the NativeArray to avoid memory leaks
    }

    public NativeList<EnemyData> ReturnEnemyDataList() { return enemyDataList; }

    public void AddEnemyData(EnemyData _enemyData) { enemyDataList.Add(_enemyData); }

    public void AddBulletData(BulletData _bulletData)
    {
        bulletDataList.Add(_bulletData);
    }
}


[BurstCompile]
public struct UpdateEnemyTargetPos : IJobParallelFor
{
    public NativeArray<EnemyData> EnemyData;
    [ReadOnly] public NativeArray<EnemyData> EnemyDataOffset;
    [ReadOnly] public NativeArray<FlowGridNode> FlowGridArray;

    // A seed that you can set from your main thread
    public uint Seed;

    public void Execute(int index)

    {
        uint seed = Seed < 1 ? 1 : Seed;

        // Get the current enemy data.
        EnemyData enemy = EnemyData[index];

        if (!enemy.TargetNeeded) { return; }

        Vector3 currentPos = enemy.Position;
        int currentIndex = GetGridIndex(currentPos);
        Vector3 flowDir;

        if (currentIndex < 0 || currentIndex >= FlowGridArray.Length)
        {
            flowDir = currentPos + GetStepTowardsCenter(currentPos);

        } else {
            // Determine the next grid node via the flow field.
            int adjustedIndex = (FlowGridArray[currentIndex].goToIndex < 0)

                ? GetGridIndex(currentPos + GetStepTowardsCenter(currentPos))
                : FlowGridArray[currentIndex].goToIndex;
            int nextIndex = FlowGridArray[adjustedIndex].goToIndex;

            // Calculate the base target position.
            flowDir = (nextIndex < 0)
                ? currentPos + GetStepTowardsCenter(currentPos)
                : FlowGridArray[nextIndex].position;
        }

        Vector3 separationForce = Vector3.zero;
        for (int i = 0; i < EnemyDataOffset.Length; i++) {
            if (Vector3.Distance(EnemyDataOffset[i].Position, enemy.Position) > 2f) { continue; }
            Vector3 away = enemy.Position - EnemyDataOffset[i].Position;
            if(away.magnitude > 0)
                separationForce += away.normalized / away.magnitude;
        }

        // Optionally, add a small random "wander" force.
        Unity.Mathematics.Random random = new Unity.Mathematics.Random(seed + (uint)index);
        Vector3 wanderForce = new Vector3(
            random.NextFloat(-0.1f, 0.1f),
            0,
            random.NextFloat(-0.1f, 0.1f)
        );


        // Blend the forces (tweak these weights as needed).
        Vector3 desiredDirection = flowDir + (separationForce * 0.4f) + (wanderForce * 0.3f);

        // Write the modified enemy data back.
        enemy.TargetPos = desiredDirection;
        EnemyData[index] = enemy;
    }

    // Converts a world position to a grid index.
    private int GetGridIndex(Vector3 pos)
    {
        return Mathf.FloorToInt(pos.z) + Mathf.FloorToInt(pos.x) * 200;
    }

    // Returns a simple step towards the center of the map.
    private Vector3 GetStepTowardsCenter(Vector3 pos)
    {
        Vector3 center = new Vector3(100, 0, 100);
        Vector3 direction = (center - pos).normalized;
        // This gives an integer-like step (e.g., (-1, 0, 1)) based on the direction.
        return new Vector3(Mathf.Sign(direction.x), 0, Mathf.Sign(direction.z));
    }
}


[BurstCompile]
public struct UpdateEnemyPosition : IJobParallelFor
{
    public NativeArray<EnemyData> EnemyData;

    [ReadOnly] public NativeArray<Vector3> ObstructedPositions;
    [ReadOnly] public NativeArray<Vector3> ShieldPositions;
    [ReadOnly] public float DeltaTime;

    public void Execute(int index)
    {
        EnemyData enemy = EnemyData[index];

        bool isAtShield = false;
        bool isObstructed = false;

        for (int j = 0; j < ShieldPositions.Length; j++)
        {
            if (Vector3.Distance(enemy.Position, ShieldPositions[j]) <= 0.2f)
            {
                isAtShield = true;
                isObstructed = true;
                break;
            }
        }

        if (!isAtShield) // so that enemies always damage the shield first not a building poking out
        {
            for (int j = 0; j < ObstructedPositions.Length; j++)
            {
                if (Vector3.Distance(enemy.Position, ObstructedPositions[j]) <= 0.2f)
                {
                    enemy.AttackPos = ObstructedPositions[j];
                    isObstructed = true;
                    break;
                }
                else
                {
                    enemy.AttackPos = default;
                }
            }
        }


        enemy.IsAtShield = isAtShield;
        enemy.IsActive = !isObstructed;

        if (!enemy.ToRemove && enemy.IsActive)
        {
            if (Vector3.Distance(enemy.Position, enemy.TargetPos) > 0.1f)
            {
                Vector3 dir = (enemy.TargetPos - enemy.Position).normalized;

                enemy.Position += (DeltaTime * enemy.Speed * dir);

                enemy.Rotation = Quaternion.LookRotation(dir);

                enemy.TargetNeeded = false;
            }
            else
            {
                enemy.TargetNeeded = true;
            }
        }

        EnemyData[index] = enemy;
    }
}

[BurstCompile]
public struct UpdateBulletPosition : IJobParallelFor
{
    public NativeArray<BulletData> BulletList;

    [ReadOnly]
    public float DeltaTime;

    public void Execute(int index)
    {
        BulletData bullet = BulletList[index];
        //bullet.Position += bullet.Velocity * DeltaTime * bullet.Speed;
        bullet.Position += ReturnPosition(bullet.Velocity, bullet.Speed);
        bullet.Lifetime += DeltaTime;
        if (bullet.Lifetime > 3f) { bullet.ToRemove = true; }
        BulletList[index] = bullet;
    }

    public Vector3 ReturnPosition(Vector3 _velocity, float _speed)
    {
        return _velocity * _speed * DeltaTime;
    }
}

public struct CollisionData
{
    public int EnemyIndex;
    public int BulletIndex;
    public int BulletDamage;
    public int TurretID;
    public BulletType BulletType;
}

public struct LightningVFXData
{
    public Vector3 StartPos;  // Start position of the bolt (enemy hit)
    public Vector3 EndPos;    // End position (next enemy hit in the chain)
    public float Duration;    // Duration of the VFX
}

public struct SpreadData
{
    public float3 Position;  
    public float3 Direction;    
}

[BurstCompile]
public struct BulletCollision : IJobParallelFor
{
    [ReadOnly] public NativeArray<EnemyData> EnemyData;
    [ReadOnly] public NativeArray<Vector3> TerrainData;
    [ReadOnly] public int ExplosionRadius;
    [ReadOnly] public int ChainRadius;     // Radius for chaining the lightning
    [ReadOnly] public int MaxChainTargets; // Maximum number of chain hits
    [ReadOnly] public float ChainDamageReductionFactor; // Damage reduction per chain


    public NativeArray<BulletData> BulletData;
    public NativeQueue<CollisionData>.ParallelWriter CollisionQueue;
    public NativeQueue<LightningVFXData>.ParallelWriter VFXQueue;
    public NativeQueue<int>.ParallelWriter EnemyIndexSlow;
    public NativeQueue<float3>.ParallelWriter ImpactPositions;
    public NativeQueue<BulletData>.ParallelWriter CreateBulletData;

    public void Execute(int index)
    {
        BulletData bullet = BulletData[index];
        Vector3 bulletPos = bullet.Position - new Vector3(0, 0.5f, 0); // Offset bullet position for height

        for (int i = 0; i < EnemyData.Length; i++)
        {
            if (Vector3.Distance(bulletPos, EnemyData[i].Position) < 1f)
            {
                if (bullet.hitEnemies.Contains(EnemyData[i].EnemyID))
                {
                    continue; // Skip enemies already hit
                }

                bullet.hitEnemies.Add(EnemyData[i].EnemyID);
                EnqueueCollision(index, bullet, i);

                // Handle specific effects based on bullet types
                if (bullet.Type == BulletType.Slow || bullet.TypeTwo == BulletType.Slow)
                {
                    EnemyIndexSlow.Enqueue(i);
                }

                if (bullet.Type == BulletType.Explosive || bullet.TypeTwo == BulletType.Explosive)
                {
                    ImpactPositions.Enqueue(bullet.Position);

                    for (int j = 0; j < EnemyData.Length; j++)
                    {
                        if (Vector3.Distance(bulletPos, EnemyData[j].Position) < ExplosionRadius)
                        {
                            EnqueueCollision(index, bullet, j);
                            bullet.ToRemove = true;
                        }
                    }
                }

                if (bullet.Type == BulletType.Spread || bullet.TypeTwo == BulletType.Spread)
                {
                    bullet.ToRemove = true;
                    CreateBulletData.Enqueue(bullet);
                }

                if (bullet.Type == BulletType.ChainLightning || bullet.TypeTwo == BulletType.ChainLightning)
                {
                    bullet.ToRemove = true;

                    EnemyData eData = EnemyData[i];

                    int chainCount = 0;
                    float currentDamage = bullet.Damage;
                    Vector3 lastHitPos = eData.Position;

                    while (chainCount < MaxChainTargets && currentDamage > 0)
                    {
                        float closestDistance = float.MaxValue;
                        int closestEnemyIndex = -1;

                        // Find the closest enemy within the chain radius
                        for (int j = 0; j < EnemyData.Length; j++)
                        {
                            if (bullet.hitEnemies.Contains(EnemyData[j].EnemyID) || EnemyData[j].EnemyID == eData.EnemyID) { continue; }

                            float distance = Vector3.Distance(bullet.Position, EnemyData[j].Position + new Vector3(0, 0.5f, 0));

                            if (distance < ChainRadius && distance < closestDistance)
                            {
                                closestDistance = distance;
                                closestEnemyIndex = j;
                            }
                        }

                        // If a valid enemy is found, apply chain damage
                        if (closestEnemyIndex != -1)
                        {
                            EnqueueCollision(index, bullet, closestEnemyIndex);

                            // Queue lightning VFX between the last hit and the new target
                            LightningVFXData vfxData = new LightningVFXData
                            {
                                StartPos = lastHitPos,
                                EndPos = EnemyData[closestEnemyIndex].Position + new Vector3(0, 0.5f, 0),
                                Duration = 0.5f // Example duration for the lightning effect
                            };
                            VFXQueue.Enqueue(vfxData);

                            currentDamage *= ChainDamageReductionFactor;

                            bullet.hitEnemies.Add(EnemyData[closestEnemyIndex].EnemyID);

                            lastHitPos = EnemyData[closestEnemyIndex].Position + new Vector3(0, 0.5f, 0);

                            chainCount++;
                        }
                        else
                        {
                            // No valid enemy found within the chain radius, stop chaining
                            break;
                        }
                    }
                }

                if (bullet.hitEnemies.Length > bullet.PassThrough)
                {
                    bullet.ToRemove = true;
                }
            }
        }


        if (bullet.Type == BulletType.OrbitalStrike)
        {
            if (bulletPos.y < 0.5f)
            {
                // Explosive logic: Affect enemies within the explosion radius
                for (int j = 0; j < EnemyData.Length; j++)
                {
                    if (Vector3.Distance(bulletPos, EnemyData[j].Position) < 5)
                    {
                        EnqueueCollision(index, bullet, j);
                    }
                }
                bullet.ToRemove = true;
            }
        }

        if (bullet.Type == BulletType.MeteorShower)
        {
            if (bulletPos.y < 0.5f)
            {
                for (int j = 0; j < EnemyData.Length; j++)
                {
                    if (Vector3.Distance(bulletPos, EnemyData[j].Position) < 5)
                    {
                        EnqueueCollision(index, bullet, j);
                    }
                }
                bullet.ToRemove = true;
            }
        }

        if (bullet.Type == BulletType.FirestormPayload)
        {
            if (bulletPos.y < 0.5f)
            {
                for (int j = 0; j < EnemyData.Length; j++)
                {
                    if (Vector3.Distance(bulletPos, EnemyData[j].Position) < 1)
                    {
                        EnqueueCollision(index, bullet, j);
                    }
                }
                bullet.ToRemove = true;
                CreateBulletData.Enqueue(bullet);
            }
        }

        if (bullet.Type == BulletType.Firestorm)
        {
            if (bulletPos.y < 0.5f)
            {
                for (int j = 0; j < EnemyData.Length; j++)
                {
                    if (Vector3.Distance(bulletPos, EnemyData[j].Position) < 5)
                    {
                        EnqueueCollision(index, bullet, j);
                    }
                }
            }
        }

        if (bullet.Type == BulletType.TimewarpPayload)
        {
            if (bulletPos.y < 0.5f)
            {
                for (int j = 0; j < EnemyData.Length; j++)
                {
                    if (Vector3.Distance(bulletPos, EnemyData[j].Position) < 1)
                    {
                        EnqueueCollision(index, bullet, j);
                    }
                }
                bullet.ToRemove = true;
                CreateBulletData.Enqueue(bullet);
            }
        }


        if (bullet.Type == BulletType.Timewarp)
        {
            if (bulletPos.y < 0.5f)
            {
                for (int j = 0; j < EnemyData.Length; j++)
                {
                    if (Vector3.Distance(bulletPos, EnemyData[j].Position) < 5)
                    {
                        EnqueueCollision(index, bullet, j);
                        EnemyIndexSlow.Enqueue(j);
                    }
                }
            }
        }


        if (!bullet.ToRemove)
        {
            for (int i = 0; i < TerrainData.Length; i++)
            {
                if (Vector3.Distance(bulletPos, TerrainData[i]) < 1f)
                {
                    if (bullet.Type == BulletType.Ricochet)
                    {
                        Vector3 newDirection = bullet.Velocity * -1;
                        bullet.Velocity = newDirection;
                    }
                    else
                    {
                        bullet.ToRemove = true;
                        break;
                    }
                }
            }
        }

        BulletData[index] = bullet;
    }

    private void EnqueueCollision(int bulletIndex, BulletData bullet, int enemyIndex)
    {
        var colData = new CollisionData
        {
            EnemyIndex = enemyIndex,
            BulletIndex = bulletIndex,
            BulletDamage = bullet.Damage,
            TurretID = bullet.TurretID,
            BulletType = bullet.Type
        };
        CollisionQueue.Enqueue(colData);
    }
}

[BurstCompile]
public struct EnemyCollisionData : IJob
{
    public NativeQueue<CollisionData> CollisionQueue;
    public NativeArray<EnemyData> EnemyDataArray;
    public NativeList<int> TurretIDList;
    [ReadOnly] public float DeltaTime;

    public void Execute()
    {
        while (CollisionQueue.TryDequeue(out CollisionData colData))
        {
            EnemyData tempEnemy = EnemyDataArray[colData.EnemyIndex];

            if (colData.BulletType == BulletType.Firestorm || colData.BulletType == BulletType.Timewarp)
            {
                tempEnemy.Health -= colData.BulletDamage * DeltaTime;
            } else
            {
                tempEnemy.Health -= colData.BulletDamage;
            }
            

            if (tempEnemy.Health <= 0)
            {
                tempEnemy.ToRemove = true;
                TurretIDList.Add(colData.TurretID);
            }

            EnemyDataArray[colData.EnemyIndex] = tempEnemy;
        }
    }
}


public struct BuildingCollisionData
{
    public float Damage;
    public Vector3 GridPosition;
}

[BurstCompile]
public struct BuildingCollision : IJobParallelFor
{
    [ReadOnly] public NativeArray<EnemyData> EnemyDataArray;
    public NativeArray<BuildingCollisionData> BuildingCollisionDataArray;
    [ReadOnly] public float DeltaTime;

    public void Execute(int index)
    {
        EnemyData eData = EnemyDataArray[index];

        if (!eData.IsActive)
        {
            BuildingCollisionData bData = new()
            {
                Damage = eData.Damage * DeltaTime,
                GridPosition = eData.AttackPos
            };

            BuildingCollisionDataArray[index] = bData;
        }
    }
}


[BurstCompile]
public struct ShieldCollisionJob : IJob
{
    [ReadOnly] public NativeArray<EnemyData> EnemyDataArray;
    [ReadOnly] public float DeltaTime;

    public NativeArray<float> ShieldDamageAmount;

    public void Execute()
    {
        float totalDamage = 0f;

        for (int i = 0; i < EnemyDataArray.Length; i++)
        {
            if (EnemyDataArray[i].IsAtShield)
            {
                totalDamage += EnemyDataArray[i].Damage * DeltaTime; 
            }
        }

        ShieldDamageAmount[0] = totalDamage;
    }
}


[BurstCompile]
public struct CheckActiveEnemies : IJob
{
    public NativeList<EnemyData> EnemyData;
    public NativeList<int> IndexToRemove;

    public void Execute()
    {
        for (int i = 0; i < EnemyData.Length; i++)
        {
            if (EnemyData[i].ToRemove)
            {
                EnemyData.RemoveAt(i);
                IndexToRemove.Add(i);
            }
        }
    }
}

[BurstCompile]
public struct CheckActiveBullets : IJob
{
    public NativeList<BulletData> BulletData;
    public NativeList<int> IndexToRemove;

    public void Execute()
    {
        for (int i = 0; i < BulletData.Length; i++)
        {
            if (BulletData[i].ToRemove)
            {
                BulletData.RemoveAt(i);
                IndexToRemove.Add(i);
            }
        }
    }
}
