using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using UnityEngine;

public class EnemyMovement: MonoBehaviour
{
    public void ScheduleMoveJobs(
        NativeList<EnemyData> enemyDataList,
        NativeList<BulletData> bulletDataList,
        NativeList<float3> shieldPositions,
        NativeList<float3> obstructPathList,
        NativeArray<float3> terrainDataArray,
        float deltaTime,
        uint seed,
        int separationMultiplier)
    {
        // Create temporary array for enemy offset data
        NativeArray<EnemyData> enemyDataOffset = new(enemyDataList.Length, Allocator.TempJob);
        NativeArray<int> gridCostUpdateArray = new(enemyDataList.Length, Allocator.TempJob);

        try
        {
            // Copy enemy data for separation calculations
            for (int i = 0; i < enemyDataList.Length; i++)
            {
                enemyDataOffset[i] = enemyDataList[i];
            }


            var updateEnemyTargetJob = new FindEnemyTargetPos
            {
                EnemyData = enemyDataList.AsArray(),
                FlowGridArray = PrecomputedData.gridArray,
                ShieldPositions = shieldPositions.AsArray(),
                BuildingPositions = obstructPathList.AsArray(),
                gridCostUpdate = gridCostUpdateArray,
                DeltaTime = deltaTime 
            };
            JobHandle targetPosHandle = updateEnemyTargetJob.Schedule(enemyDataList.Length, 64);
            targetPosHandle.Complete();

            var updateNodeCostJob = new UpdateNodeCost
            {
                GridCostUpdate = gridCostUpdateArray,
                FlowGridArray = PrecomputedData.gridArray,
            };
            JobHandle updateNodeCostHandle = updateNodeCostJob.Schedule();
            updateNodeCostHandle.Complete();
        }
        finally
        {
            gridCostUpdateArray.Dispose();
            // Clean up temporary array
            if (enemyDataOffset.IsCreated)
            {
                enemyDataOffset.Dispose();
            }
        }
    }
}