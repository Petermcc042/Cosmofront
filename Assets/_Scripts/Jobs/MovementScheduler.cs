using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using UnityEngine;

public class MovementScheduler
{
    public void ScheduleMoveJobs(
        NativeList<EnemyData> enemyDataList,
        NativeList<BulletData> bulletDataList,
        NativeList<float3> shieldPositions,
        NativeList<float3> obstructPathList,
        NativeArray<FlowGridNode> flowNodes,
        NativeArray<float3> terrainDataArray,
        float deltaTime,
        uint seed)
    {
        // Create temporary array for enemy offset data
        NativeArray<EnemyData> enemyDataOffset = new(enemyDataList.Length, Allocator.TempJob);

        try
        {
            // Copy enemy data for separation calculations
            for (int i = 0; i < enemyDataList.Length; i++)
            {
                enemyDataOffset[i] = enemyDataList[i];
            }


            var updateEnemyTargetJob = new UpdateEnemyTargetPos
            {
                EnemyData = enemyDataList.AsArray(),
                TerrainDataArray = terrainDataArray,
                EnemyDataOffset = enemyDataOffset,
                FlowGridArray = flowNodes,
                Seed = seed
            };
            JobHandle targetPosHandle = updateEnemyTargetJob.Schedule(enemyDataList.Length, 64);
            targetPosHandle.Complete();

            var moveEnemyJob = new UpdateEnemyPosition
            {
                EnemyData = enemyDataList.AsArray(),
                ShieldPositions = shieldPositions.AsArray(),
                ObstructedPositions = obstructPathList.AsArray(),
                DeltaTime = deltaTime
            };
            var bulletMoveJob = new UpdateBulletPosition
            {
                BulletList = bulletDataList.AsArray(),
                DeltaTime = deltaTime
            };

            JobHandle combinedHandle = JobHandle.CombineDependencies(
                moveEnemyJob.Schedule(enemyDataList.Length, 64),
                bulletMoveJob.Schedule(bulletDataList.Length, 64)
            );

            combinedHandle.Complete();
        }
        finally
        {
            // Clean up temporary array
            if (enemyDataOffset.IsCreated)
            {
                enemyDataOffset.Dispose();
            }
        }
    }
}