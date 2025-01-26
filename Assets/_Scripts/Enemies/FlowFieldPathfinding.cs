using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;


public struct FlowGridNode
{
    public int index;
    public int x;
    public int z;
    public int cost;             // Movement cost (terrain difficulty)
    public int integrationCost;  // Cumulative cost to reach the target
    //public Vector2 flowDirection; // Normalized vector for movement direction
    public bool isWalkable;
    public bool isOutsideBase;
    public bool isBuilding;
    public int goToIndex; // Index of the parent node for path reconstruction
}

[BurstCompile]
public struct UpdateNodesMovementCost : IJobParallelFor
{
    public NativeArray<FlowGridNode> GridArray;
    [ReadOnly] public int endX, endZ;
    [ReadOnly] public bool runFullGrid;

    public void Execute(int index)
    {
        FlowGridNode tempNode = GridArray[index];
        if (!runFullGrid && tempNode.isOutsideBase) { return; }
        tempNode.integrationCost = int.MaxValue; // node max distance can be no further than 500
        tempNode.cost = CalculateDistanceCost(tempNode.x, tempNode.z, endX, endZ);
        tempNode.goToIndex = -1; // Reset cameFromIndex
        GridArray[index] = tempNode;
    }

    private int CalculateDistanceCost(int x1, int z1, int x2, int z2)
    {
        int dx = math.abs(x1 - x2);
        int dz = math.abs(z1 - z2);
        int remaining = math.abs(dx - dz);
        return (14 * math.min(dx, dz)) + (10 * remaining); // Diagonal and straight costs
    }
}


[BurstCompile]
public struct UpdateNodesIntegration : IJob
{
    public NativeArray<FlowGridNode> GridArray;
    public NativeQueue<FlowGridNode> NodeQueue;
    [ReadOnly] public int endX, endZ;
    [ReadOnly] public int gridWidth;
    [ReadOnly] public bool runFullGrid;

    public void Execute()
    {
        // Start from the target
        int targetIndex = GetIndex(endX, endZ);
        FlowGridNode targetCell = GridArray[targetIndex];
        targetCell.integrationCost = 0;
        GridArray[targetIndex] = targetCell;


        // Breadth-first search (BFS) to calculate integration cost
        NodeQueue.Enqueue(targetCell);

        while (NodeQueue.Count > 0)
        {
            FlowGridNode currentCell = NodeQueue.Dequeue();


            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                for (int offsetZ = -1; offsetZ <= 1; offsetZ++)
                {
                    if (offsetX == 0 && offsetZ == 0) continue; // Skip the current node

                    int neighborX = currentCell.x + offsetX;
                    int neighborZ = currentCell.z + offsetZ;

                    // Bounds check
                    if (neighborX >= 0 && neighborX < gridWidth && neighborZ >= 0 && neighborZ < gridWidth)
                    {
                        FlowGridNode neighbour = GridArray[GetIndex(neighborX, neighborZ)];
                        if (!neighbour.isWalkable) continue; // Skip impassable cells
                        if (!runFullGrid && neighbour.isOutsideBase) { continue; } // Skip impassable cells

                        // if zero + the direct movement cost < max int value se the integration cost to this new value
                        int newCost = currentCell.integrationCost + neighbour.cost;
                        if (newCost < neighbour.integrationCost)
                        {
                            neighbour.integrationCost = newCost;
                            neighbour.goToIndex = currentCell.index;
                            GridArray[neighbour.index] = neighbour;
                            NodeQueue.Enqueue(neighbour);
                        }
                    }
                }
            }
        }
    }

    private int GetIndex(int x, int z)
    {
        return z + x * gridWidth;
    }
}




