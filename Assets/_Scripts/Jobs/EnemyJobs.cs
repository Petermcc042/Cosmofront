using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public struct UpdateEnemyTargetPos : IJobParallelFor
{
    public NativeArray<EnemyData> EnemyData;
    [ReadOnly] public NativeArray<EnemyData> EnemyDataOffset;
    [ReadOnly] public NativeArray<GridNode> FlowGridArray;
    [ReadOnly] public NativeArray<float3> TerrainDataArray;

    // A seed that you can set from your main thread
    public uint Seed;
    private const float TERRAIN_DETECTION_RADIUS = 2f;
    private const float TERRAIN_FORCE_MULTIPLIER = 2.5f;
    private const float MIN_TERRAIN_DISTANCE = 1f;

    public void Execute(int index)
    {
        uint seed = Seed < 1 ? 1 : Seed;
        EnemyData enemy = EnemyData[index];
        //if (!enemy.TargetNeeded) { return; }

        float3 currentPos = enemy.Position;
        int currentIndex = GetGridIndex(currentPos);

        float3 flowDir = CalculateFlowDirection(currentPos, currentIndex);

        float3 separationForce = CalculateEnemySeparation(enemy);

        float3 terrainSeparationForce = float3.zero;//CalculateTerrainSeparation(enemy.Position);

        float3 wanderForce = CalculateWanderForce(seed, index);

        float3 desiredDirection = flowDir + (separationForce * 0.4f) + (wanderForce * 0.3f);// + (terrainSeparationForce);

        enemy.TargetPos = desiredDirection;
        EnemyData[index] = enemy;
    }

    private float3 CalculateFlowDirection(float3 currentPos, int currentIndex)
    {
        if (currentIndex < 0 || currentIndex >= FlowGridArray.Length)
        {
            return currentPos + GetStepTowardsCenter(currentPos);
        }

        int adjustedIndex = (FlowGridArray[currentIndex].goToIndex < 0)
            ? GetGridIndex(currentPos + GetStepTowardsCenter(currentPos))
            : FlowGridArray[currentIndex].goToIndex;

        int nextIndex = FlowGridArray[adjustedIndex].goToIndex;

        return (nextIndex < 0)
            ? currentPos + GetStepTowardsCenter(currentPos)
            : FlowGridArray[nextIndex].position;
    }

    private float3 CalculateEnemySeparation(EnemyData enemy)
    {
        float3 separationForce = float3.zero;
        for (int i = 0; i < EnemyDataOffset.Length; i++)
        {
            if (math.distance(EnemyDataOffset[i].Position, enemy.Position) > 2f) { continue; }

            float3 away = enemy.Position - EnemyDataOffset[i].Position;
            float magnitude = math.length(away);
            if (magnitude > 0)
            {
                separationForce += math.normalize(away) / magnitude;
            }
        }
        return separationForce;
    }

    private float3 CalculateTerrainSeparation(float3 position)
    {
        float3 separationForce = float3.zero;
        for (int i = 0; i < TerrainDataArray.Length; i++)
        {
            float distance = math.distance(position, TerrainDataArray[i]);
            if (distance > TERRAIN_DETECTION_RADIUS) { continue; }

            float3 away = position - TerrainDataArray[i];
            float magnitude = math.length(away);
            if (magnitude > 0)
            {
                // Exponential force increase as distance decreases
                float forceMagnitude = 1f / (magnitude * magnitude);
                separationForce += math.normalize(away) * forceMagnitude;
            }
        }
        return separationForce;
    }

    private float3 CalculateWanderForce(uint seed, int index)
    {
        Unity.Mathematics.Random random = new Unity.Mathematics.Random(seed + (uint)index);
        return new float3(random.NextFloat(-0.1f, 0.1f), 0, random.NextFloat(-0.1f, 0.1f));
    }

    // Converts a world position to a grid index.
    private int GetGridIndex(float3 pos)
    {
        return (int)math.floor(pos.z) + (int)math.floor(pos.x) * 200;
    }

    // Returns a simple step towards the center of the map.
    private float3 GetStepTowardsCenter(float3 pos)
    {
        float3 center = new float3(100, 0, 100);
        float3 direction = math.normalize(center - pos);
        // This gives an integer-like step (e.g., (-1, 0, 1)) based on the direction
        return new float3(math.sign(direction.x), 0, math.sign(direction.z));
    }
}


[BurstCompile]
public struct UpdateEnemyPosition : IJobParallelFor
{
    public NativeArray<EnemyData> EnemyData;

    [ReadOnly] public NativeArray<float3> ObstructedPositions;
    [ReadOnly] public NativeArray<float3> ShieldPositions;
    [ReadOnly] public float DeltaTime;

    public void Execute(int index)
    {
        EnemyData enemy = EnemyData[index];

        bool isAtShield = false;
        bool isObstructed = false;

        for (int j = 0; j < ShieldPositions.Length; j++)
        {
            if (math.distance(enemy.Position, ShieldPositions[j]) <= 0.2f)
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
                if (math.distance(enemy.Position, ObstructedPositions[j]) <= 0.2f)
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
            if (math.distance(enemy.Position, enemy.TargetPos) > 0.1f)
            {
                float3 dir = math.normalize(enemy.TargetPos - enemy.Position);

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



public struct TurretUpgradeData {
    public int XPAmount;
    public int TurretID;
}

[BurstCompile]
public struct EnemyCollisionData : IJob
{
    public NativeQueue<CollisionData> CollisionQueue;
    public NativeArray<EnemyData> EnemyDataArray;
    public NativeList<TurretUpgradeData> TurretUpgradeDataList;
    [ReadOnly] public float DeltaTime;

    public void Execute()
    {
        while (CollisionQueue.TryDequeue(out CollisionData colData))
        {
            EnemyData tempEnemy = EnemyDataArray[colData.EnemyIndex];

            if (colData.BulletType == BulletType.Firestorm || colData.BulletType == BulletType.Timewarp)
            {
                tempEnemy.Health -= colData.BulletDamage * DeltaTime;
            }
            else
            {
                tempEnemy.Health -= colData.BulletDamage;
            }


            if (tempEnemy.Health <= 0)
            {
                tempEnemy.ToRemove = true;
                TurretUpgradeData tempUpgrade = new()
                {
                    XPAmount = 10,
                    TurretID = colData.TurretID
                };

                TurretUpgradeDataList.Add(tempUpgrade);
                    
            }

            EnemyDataArray[colData.EnemyIndex] = tempEnemy;
        }
    }
}


public struct BuildingCollisionData
{
    public float Damage;
    public float3 GridPosition;
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