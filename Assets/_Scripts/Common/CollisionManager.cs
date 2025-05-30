using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class CollisionManager : MonoBehaviour
{
    [SerializeField] private EnemyManager enemyManager;
    [SerializeField] private EnemyMovement enemyMovement;
    [SerializeField] private BulletManager bulletManager;
    [SerializeField] private MapGridManager mapGridManager;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private Generator gen;
    [SerializeField] private TurretManager turretManager;
    [SerializeField] private TerrainGen terrainGen;

    [SerializeField] private Slider genHealth;
    [SerializeField] private int explosionRadius;
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private int separataionForceMultiplier;

    private NativeList<float3> buildingPositionList;
    private NativeList<float3> shieldPositions;

    private NativeList<EnemyData> enemyDataList;
    private NativeList<BulletData> bulletDataList;
    private NativeArray<float3> terrainDataArray;

    uint mySeed = 1;

    private List<(GameObject obj, float timer)> lightningVFX = new List<(GameObject obj, float timer)>();
    private const float LIGHTNING_DURATION = 0.1f;

    private void Start()
    {
        buildingPositionList = mapGridManager.buildingGridSquareList;
        shieldPositions = gen.shieldGridSquareList;
        enemyDataList = enemyManager.enemyDataList;
        bulletDataList = bulletManager.bulletDataList;
    }

    private void OnDestroy()
    {
        terrainDataArray.Dispose();
    }

    public void CreateTerrainArray()
    {
        terrainDataArray = terrainGen.blockedCoordsArray;
    }

    public void CallUpdate()
    {
        float deltaTime = Time.deltaTime;

        enemyMovement.ScheduleMoveJobs(enemyDataList, bulletDataList, shieldPositions, buildingPositionList, terrainDataArray, deltaTime, mySeed, separataionForceMultiplier);        
        bulletManager.UpdateBulletPositions(deltaTime);

        CheckBulletCollisions();
        RemovalAndUpdate();
        DamageBuildings();
        UpdateLightningVFX();
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
            BulletData = bulletDataList.AsArray(),
            CollisionQueue = colData.AsParallelWriter(),
            VFXQueue = lightningData.AsParallelWriter(),
            EnemyIndexSlow = enemySlowData.AsParallelWriter(),
            ImpactPositions = impactPositions.AsParallelWriter(),
            CreateBulletData = createBulletData.AsParallelWriter()
        };

        JobHandle bulletHandle = bulletCollisionJob.Schedule(bulletDataList.Length, 64);
        bulletHandle.Complete();


        NativeList<TurretUpgradeData> turretXP = new(Allocator.Persistent);

        var collisionJob = new EnemyCollisionData
        {
            EnemyDataArray = enemyDataList.AsArray(),
            CollisionQueue = colData,
            TurretUpgradeDataList = turretXP,
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
                float magnitude = math.length(_data.Velocity);
                Vector3 originalDirection = magnitude > 0 ? _data.Velocity / magnitude : float3.zero;

                for (int i = 0; i < 3; i++)
                {
                    float spreadAngle = (i - 1) * 0.5f; // Adjust spread factor as needed
                    Vector3 spreadDirection = Quaternion.AngleAxis(spreadAngle * 30f, Vector3.up) * originalDirection; // Rotate around Y axis for fan-out

                    bulletManager.SpawnBullet(_data.Position, spreadDirection * math.length(_data.Velocity), 10, _data.TurretID, _data.Damage, 0, BulletType.Standard);
                }
            }
            else if (_data.Type == BulletType.Circler)
            {
                float magnitude = math.length(_data.Velocity);
                Vector3 baseDirection = magnitude > 0 ? _data.Velocity / magnitude : Vector3.forward; // Default to forward if no velocity

                for (int i = 0; i < 8; i++)
                {
                    float angle = i * 45f; // Spread evenly in 8 directions (360 degrees)
                    Vector3 direction = Quaternion.Euler(0, angle, 0) * baseDirection;

                    bulletManager.SpawnBullet(_data.Position, direction * magnitude, 10, _data.TurretID, _data.Damage, 0, BulletType.Standard);
                }
            }
            else if (_data.Type == BulletType.SpreadCircles)
            {
                float magnitude = math.length(_data.Velocity);
                Vector3 baseDirection = magnitude > 0 ? _data.Velocity / magnitude : Vector3.forward; // Default to forward if no velocity

                for (int i = 0; i < 8; i++)
                {
                    float angle = i * 45f; // Spread evenly in 8 directions (360 degrees)
                    Vector3 direction = Quaternion.Euler(0, angle, 0) * baseDirection;

                    bulletManager.SpawnBullet(_data.Position, direction * magnitude, 10, _data.TurretID, _data.Damage, 0, BulletType.Spread);
                }
            }
            else if (_data.Type == BulletType.FirestormPayload)
            {
                bulletManager.SpawnBullet(_data.Position, Vector3.zero, 0, _data.TurretID, 1, 0, BulletType.Firestorm);
            }
            else if (_data.Type == BulletType.Slow)
            {
                bulletManager.SpawnBullet(_data.Position, Vector3.zero, 0, _data.TurretID, 1, 0, BulletType.Timewarp);
            }
            else
            {
                bulletManager.SpawnBullet(_data.Position, Vector3.zero, 0, _data.TurretID, 1, 0, BulletType.Standard);
            }
        }

        turretManager.HandleXPUpdate(turretXP);

        turretXP.Dispose();
        colData.Dispose();
        impactPositions.Dispose();
        lightningData.Dispose();
        enemySlowData.Dispose();
        createBulletData.Dispose();
    }

    private void UpdateLightningVFX()
    {
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

        enemyManager.UpdateEnemyPositions(enemyToRemove);
        bulletManager.UpdateBulletData(bulletToRemove);

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
            PlacedObject tempObject = PrecomputedData.GetPlacedObject(tempData.GridPosition);
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

                    PrecomputedData.GetXZ(tempData.GridPosition, out int _x, out int _z);
                    mapGridManager.DestroyBuilding(_x, _z);
                }
            }
        }

        buildingCollisionDataArray.Dispose();

        // Retrieve the calculated shield damage
        gen.DamageLoop(_tempShieldDamage[0], Time.deltaTime);

        _tempShieldDamage.Dispose();  // Dispose of the NativeArray to avoid memory leaks
    }
}