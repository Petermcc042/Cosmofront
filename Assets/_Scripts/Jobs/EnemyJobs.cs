using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using static PlacedObjectSO;

[BurstCompile]
public struct FindEnemyTargetPos : IJobParallelFor
{
    public NativeArray<EnemyData> EnemyData;
    public NativeArray<int> gridCostUpdate;
    [ReadOnly] public NativeArray<GridNode> FlowGridArray;
    [ReadOnly] public NativeArray<float3> BuildingPositions;
    [ReadOnly] public NativeArray<float3> ShieldPositions;
    [ReadOnly] public float DeltaTime;

    private static readonly int[] NeighborOffsetsX = { -1,  0,  1, -1, 1, -1, 0, 1 };
    private static readonly int[] NeighborOffsetsZ = { -1, -1, -1,  0, 0,  1, 1, 1 };

    public void Execute(int index)
    {
        EnemyData enemy = EnemyData[index];
        
        // Check if enemy is close enough to current waypoint to find a new one
        if (math.distance(enemy.Position, enemy.TargetPos) < 0.05f || enemy.TargetPos.Equals(float3.zero))
        {
            int gridX = (int)math.round(enemy.Position.x);
            int gridZ = (int)math.round(enemy.Position.z);

            // Find neighbor with lowest integration cost
            int bestNeighborIndex = -1;
            float lowestCost = float.MaxValue;
            float3 neighbourPos = Vector3.zero;

            // Check all neighbors
            for (int i = 0; i < 8; i++)
            {
                int neighbourX = gridX + NeighborOffsetsX[i];
                int neighbourZ = gridZ + NeighborOffsetsZ[i];

                // Get the index in the grid array
                int neighborIndex = neighbourZ + neighbourX * 200;

                // Make sure the neighbor index is valid
                if (neighborIndex >= 0 && neighborIndex < FlowGridArray.Length)
                {
                    float neighborCost = FlowGridArray[neighborIndex].integrationCost + FlowGridArray[neighborIndex].movementCost * 10;

                    // Find the neighbor with the lowest cost
                    if (neighborCost < lowestCost)
                    {
                        lowestCost = neighborCost;
                        bestNeighborIndex = neighborIndex;
                        neighbourPos = new float3(neighbourX, 0f, neighbourZ);
                    }
                }
            }

            enemy.TargetPos = FlowGridArray[bestNeighborIndex].position;

            if (neighbourPos.x > 20 && neighbourPos.x < 180 && neighbourPos.z > 20 && neighbourPos.z < 180)
            {
                gridCostUpdate[index] = bestNeighborIndex;
            }
        }

        // Check if enemy is hitting something
        bool isAtShield = false;
        bool isAttacking = false;

        // Check shield first so they damage it not a building poking out
        for (int j = 0; j < ShieldPositions.Length; j++)
        {
            if (math.distance(enemy.Position, ShieldPositions[j]) < 0.6f)
            {
                isAtShield = true;
                isAttacking = true;
                break;
            }
        }

        // If we aren't at the shield check if there are other buildings obstructing
        if (!isAtShield)
        {
            for (int j = 0; j < BuildingPositions.Length; j++)
            {
                if (math.distance(enemy.Position, BuildingPositions[j]) < 0.5f)
                {
                    enemy.AttackPos = BuildingPositions[j];
                    isAttacking = true;
                    break;
                }
                else
                {
                    enemy.AttackPos = float3.zero;
                }
            }
        }

        enemy.IsAtShield = isAtShield;
        enemy.IsAttacking = isAttacking;

        float3 directionVector = enemy.TargetPos - enemy.Position;
        float3 dir = math.normalize(directionVector);

        enemy.Rotation = Quaternion.LookRotation(dir);
        if (!isAttacking)
        {
            enemy.Position += (DeltaTime * enemy.Speed * dir);
        }

        EnemyData[index] = enemy;
    }
}


[BurstCompile]
public struct UpdateNodeCost : IJob
{
    public NativeArray<GridNode> FlowGridArray;
    [ReadOnly] public NativeArray<int> GridCostUpdate;

    public void Execute()
    {
        for (int index = 0; index < GridCostUpdate.Length; index++)
        {
            GridNode tempNode = FlowGridArray[GridCostUpdate[index]];
            tempNode.movementCost += 1;
            FlowGridArray[GridCostUpdate[index]] = tempNode;
        }
    }
}

[BurstCompile]
public struct UpdateEnemyTargetPos : IJobParallelFor
{
    public NativeArray<EnemyData> EnemyData;
    //[ReadOnly] public NativeArray<EnemyData> EnemyDataOffset;
    [ReadOnly] public NativeArray<GridNode> FlowGridArray;
    [ReadOnly] public NativeArray<float3> BuildingPositions;
    [ReadOnly] public NativeArray<float3> ShieldPositions;
    [ReadOnly] public float DeltaTime;

    public void Execute(int index)
    {
        EnemyData enemy = EnemyData[index];


        // set enemies new position
        float3 currentPos = enemy.Position;
        int currentIndex = GetGridIndex(currentPos);

        if (math.distance(enemy.Position, enemy.TargetPos) < 0.05f)
        {
            if (currentIndex < 0) { return; }
            int nextIndex = FlowGridArray[currentIndex].goToIndex;
            int nextnextIndex = FlowGridArray[nextIndex].goToIndex;
            enemy.TargetPos = FlowGridArray[nextnextIndex].position;
        }
        else
        {
            if (currentIndex < 0) { return; }
            int nextIndex = FlowGridArray[currentIndex].goToIndex;
            enemy.TargetPos = FlowGridArray[nextIndex].position;
        }


        // check if enemy is hitting something
        bool isAtShield = false;
        bool isAttacking = false;

        // check shield first so they damage it not a building poking out
        for (int j = 0; j < ShieldPositions.Length; j++)
        {
            if (math.distance(enemy.Position, ShieldPositions[j]) < 0.6f)
            {
                isAtShield = true;
                isAttacking = true;
                break;
            }
        }

        // if we aren't at the shield check if there are other buildings obstructing
        if (!isAtShield)
        {
            for (int j = 0; j < BuildingPositions.Length; j++)
            {
                if (math.distance(enemy.Position, BuildingPositions[j]) < 0.5f)
                {
                    enemy.AttackPos = BuildingPositions[j];
                    isAttacking = true;
                    break;
                }
                else
                {
                    enemy.AttackPos = float3.zero;
                }
            }
        }


        enemy.IsAtShield = isAtShield;
        enemy.IsAttacking = isAttacking;

        float3 directionVector = enemy.TargetPos - enemy.Position;

        if (math.lengthsq(directionVector) > 0.001f)
        {
            float3 dir = math.normalize(directionVector);

            enemy.Rotation = Quaternion.LookRotation(dir);

            if (!isAttacking)
            {
                enemy.Position += (DeltaTime * enemy.Speed * dir);
            }
        }

        enemy.Rotation = Quaternion.LookRotation(directionVector);


        EnemyData[index] = enemy;
    }

    private int GetGridIndex(float3 pos)
    {
        return (int)(math.abs(math.floor(pos.z)) + math.abs(math.floor(pos.x)) * 200);
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

            if (tempEnemy.Health > 0)
            {
                tempEnemy.hitCount += 1;
            }

            if (tempEnemy.Health <= 0)
            {
                tempEnemy.IsDead = true;
                TurretUpgradeData tempUpgrade = new()
                {
                    XPAmount = 2,
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
    [ReadOnly] public float DeltaTime;

    public NativeArray<BuildingCollisionData> BuildingCollisionDataArray;

    public void Execute(int index)
    {
        EnemyData eData = EnemyDataArray[index];

        if (!eData.IsAttacking && !eData.IsAtShield) { return; }

        BuildingCollisionData bData = new()
        {
            Damage = eData.Damage * DeltaTime,
            GridPosition = eData.AttackPos
        };

        BuildingCollisionDataArray[index] = bData;
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
            if (EnemyData[i].IsDead)
            {
                EnemyData.RemoveAt(i);
                IndexToRemove.Add(i);
            }
        }
    }
}