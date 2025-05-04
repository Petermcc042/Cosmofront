using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class PathfindingManager : MonoBehaviour
{
    private int length;

    public void RunFlowFieldJobs(int _endX, int _endZ, bool _runFullFlow)
    {
        // Start measuring time
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();

        length = 200;

        var updateCostJob = new UpdateNodesMovementCost
        {
            GridArray = PrecomputedData.gridArray,
            endX = _endX,
            endZ = _endZ,
            runFullGrid = _runFullFlow
        };
        JobHandle handle1 = updateCostJob.Schedule(PrecomputedData.gridArray.Length, 64);
        handle1.Complete();


        NativeQueue<GridNode> nodeQueue = new NativeQueue<GridNode>(Allocator.Persistent);

        UpdateNodesIntegration updateIntegrationJob = new UpdateNodesIntegration
        {
            GridArray = PrecomputedData.gridArray,
            NodeQueue = nodeQueue,
            endX = _endX,
            endZ = _endZ,
            gridWidth = length,
            runFullGrid = _runFullFlow
        };

        JobHandle handle2 = updateIntegrationJob.Schedule();
        handle2.Complete();

        nodeQueue.Dispose();

        WeightBuildingNodes flowJob3 = new WeightBuildingNodes
        {
            GridArray = PrecomputedData.gridArray,
            endX = _endX,
            endZ = _endZ,
            gridWidth = length
        };

        JobHandle handle3 = flowJob3.Schedule();
        handle3.Complete();


        UpdateGoToIndex flowJob4 = new UpdateGoToIndex
        {
            GridArray = PrecomputedData.gridArray,
            endX = _endX,
            endZ = _endZ,
            runFullGrid = _runFullFlow,
            gridWidth = length
        };

        JobHandle flowHandle4 = flowJob4.Schedule();
        flowHandle4.Complete();

        stopwatch.Stop();
        Debug.Log($"Pathfinding: {stopwatch.ElapsedMilliseconds} ms");

        PrecomputedData.WriteDataToCSV("precomputed_grid.csv");
    }
}
