using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;


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

    public float3 ReturnPosition(float3 _velocity, float _speed)
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
    public float3 StartPos;  // Start position of the bolt (enemy hit)
    public float3 EndPos;    // End position (next enemy hit in the chain)
    public float Duration;    // Duration of the VFX
    public bool levelTwo;
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
    [ReadOnly] public NativeArray<float3> TerrainData;
    [ReadOnly] public int ExplosionRadius;

    public NativeArray<BulletData> BulletData;
    public NativeQueue<CollisionData>.ParallelWriter CollisionQueue;
    public NativeQueue<LightningVFXData>.ParallelWriter VFXQueue;
    public NativeQueue<int>.ParallelWriter EnemyIndexSlow;
    public NativeQueue<float3>.ParallelWriter ImpactPositions;
    public NativeQueue<BulletData>.ParallelWriter CreateBulletData;

    public void Execute(int index)
    {
        BulletData bullet = BulletData[index];
        float3 bulletPos = bullet.Position - new float3(0, 0.5f, 0); // Offset bullet position for height

        for (int i = 0; i < EnemyData.Length; i++)
        {
            if (math.distance(bulletPos, EnemyData[i].Position) < 1f)
            {
                if (bullet.hitEnemies.Contains(EnemyData[i].EnemyID))
                {
                    continue; // Skip enemies already hit
                }

                bullet.hitEnemies.Add(EnemyData[i].EnemyID);
                EnqueueCollision(index, bullet, i);

                // Handle specific effects based on bullet types
                if (bullet.Type == BulletType.Slow)
                {
                    EnemyIndexSlow.Enqueue(i);
                }

                if (bullet.Type == BulletType.Explosive)
                {
                    ImpactPositions.Enqueue(bullet.Position);

                    for (int j = 0; j < EnemyData.Length; j++)
                    {
                        if (math.distance(bulletPos, EnemyData[j].Position) < ExplosionRadius)
                        {
                            EnqueueCollision(index, bullet, j);
                            bullet.ToRemove = true;
                        }
                    }
                }

                if (bullet.Type == BulletType.Spread || bullet.Type == BulletType.Circler || bullet.Type == BulletType.SpreadCircles)
                {
                    bullet.ToRemove = true;
                    CreateBulletData.Enqueue(bullet);
                }

                if (bullet.Type == BulletType.ChainLightning)
                {
                    LightingCalc(bullet, index, i, 3, 3, 0.8f);
                }

                if (bullet.Type == BulletType.ArcLightning)
                {
                    LightingCalc(bullet, index, i, 10, 10, 0.8f);
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
                    if (math.distance(bulletPos, EnemyData[j].Position) < 5)
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
                    if (math.distance(bulletPos, EnemyData[j].Position) < 5)
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
                    if (math.distance(bulletPos, EnemyData[j].Position) < 1)
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
                    if (math.distance(bulletPos, EnemyData[j].Position) < 5)
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
                    if (math.distance(bulletPos, EnemyData[j].Position) < 1)
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
                    if (math.distance(bulletPos, EnemyData[j].Position) < 5)
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
                if (math.distance(bulletPos, TerrainData[i]) < 1f)
                {
                    if (bullet.Type == BulletType.Ricochet)
                    {
                        float3 newDirection = bullet.Velocity * -1;
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

    private void LightingCalc(BulletData bullet, int index, int enemyIndex, int _maxChainTargets, int _chainRadius, float _chainDamageReductionFactor)
    {
        bullet.ToRemove = true;

        EnemyData eData = EnemyData[enemyIndex];

        int chainCount = 0;
        float currentDamage = bullet.Damage;
        float3 lastHitPos = eData.Position;

        while (chainCount < _maxChainTargets && currentDamage > 0)
        {
            float closestDistance = float.MaxValue;
            int closestEnemyIndex = -1;

            // Find the closest enemy within the chain radius
            for (int j = 0; j < EnemyData.Length; j++)
            {
                if (bullet.hitEnemies.Contains(EnemyData[j].EnemyID) || EnemyData[j].EnemyID == eData.EnemyID) { continue; }

                float distance = math.distance(bullet.Position, EnemyData[j].Position + new float3(0, 0.5f, 0));

                if (distance < _chainRadius && distance < closestDistance)
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
                    EndPos = EnemyData[closestEnemyIndex].Position + new float3(0, 0.5f, 0),
                    Duration = 0.5f // Example duration for the lightning effect
                };
                VFXQueue.Enqueue(vfxData);


                currentDamage *= _chainDamageReductionFactor;

                bullet.hitEnemies.Add(EnemyData[closestEnemyIndex].EnemyID);

                lastHitPos = EnemyData[closestEnemyIndex].Position + new float3(0, 0.5f, 0);

                chainCount++;
            }
            else
            {
                // No valid enemy found within the chain radius, stop chaining
                break;
            }
        }
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
