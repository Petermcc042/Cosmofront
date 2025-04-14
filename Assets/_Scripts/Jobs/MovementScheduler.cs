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
                FlowGridArray = PrecomputedData.gridArray,
                Seed = seed
            };
            JobHandle targetPosHandle = updateEnemyTargetJob.Schedule(enemyDataList.Length, 64);
            targetPosHandle.Complete();

            var moveEnemyJob = new UpdateEnemyPosition
            {
                EnemyData = enemyDataList.AsArray(),
                ShieldPositions = shieldPositions.AsArray(),
                BuildingPositions = obstructPathList.AsArray(),
                DeltaTime = deltaTime
            };

            JobHandle jobHandle = moveEnemyJob.Schedule(enemyDataList.Length, 64);
            jobHandle.Complete();
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