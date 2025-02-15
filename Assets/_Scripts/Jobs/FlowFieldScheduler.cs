using Unity.Collections;
using Unity.Jobs;

public class FlowFieldScheduler
{
    public void ScheduleFlowFieldJobs(
        NativeArray<FlowGridNode> gridNodes, 
        int gridLength, 
        int endX, int endZ, 
        bool runFullFlow)
    {
        // Job 1: Update movement cost
        var updateCostJob = new UpdateNodesMovementCost
        {
            GridArray = gridNodes,
            endX = endX,
            endZ = endZ,
            runFullGrid = runFullFlow
        };
        JobHandle handle1 = updateCostJob.Schedule(gridNodes.Length, 64);

        // Create queue with TempJob allocator
        var nodeQueue = new NativeQueue<FlowGridNode>(Allocator.TempJob);
        
        try 
        {
            // Job 2: Update integration, scheduling after job1 finishes
            var updateIntegrationJob = new UpdateNodesIntegration
            {
                GridArray = gridNodes,
                NodeQueue = nodeQueue,
                endX = endX,
                endZ = endZ,
                gridWidth = gridLength,
                runFullGrid = runFullFlow
            };
            JobHandle handle2 = updateIntegrationJob.Schedule(handle1);


            WeightBuildingNodes flowJob3 = new WeightBuildingNodes
            {
                GridArray = gridNodes,
                endX = endX,
                endZ = endZ,
                runFullGrid = runFullFlow,
                gridWidth = gridLength
            };

            JobHandle handle3 = flowJob3.Schedule(handle2);


            UpdateGoToIndex flowJob4 = new UpdateGoToIndex
            {
                GridArray = gridNodes,
                endX = endX,
                endZ = endZ,
                runFullGrid = runFullFlow,
                gridWidth = gridLength
            };

            JobHandle flowHandle4 = flowJob4.Schedule(handle3);
            flowHandle4.Complete();
        }
        finally 
        {
            // Dispose the queue after all jobs are complete
            if (nodeQueue.IsCreated)
            {
                nodeQueue.Dispose();
            }
        }
    }
} 