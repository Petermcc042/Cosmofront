using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;


public struct GridNode
{
    public float3 position;
    public int index;
    public int x;
    public int z;
    public int cost; // Movement cost (terrain difficulty)
    public int integrationCost; // Cumulative cost to reach the target
    public int movementCost; // Cumulative cost to reach the target
    public int goToIndex; // Index of the parent node for path reconstruction
    public bool isWalkable; // is for enemy pathfinding
    public bool isPathfindingArea; // is for reducing the number of squares updated in pathfinding runs
    public bool isBuilding; // is for checking whether a node is a building or a general obstacle
    public bool isTraversable; // is for checking whether pahtfinding can reach this point
    public bool isBaseArea; // is for checking if a node is within the buildable area for a player
}



[BurstCompile]
public struct UpdateNodesMovementCost : IJobParallelFor
{
    public NativeArray<GridNode> GridArray;
    [ReadOnly] public int endX, endZ;
    [ReadOnly] public bool runFullGrid;

    public void Execute(int index)
    {
        GridNode tempNode = GridArray[index];
        if (!runFullGrid && !tempNode.isPathfindingArea) { return; }
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
    public NativeArray<GridNode> GridArray;
    public NativeQueue<GridNode> NodeQueue;
    [ReadOnly] public int endX, endZ;
    [ReadOnly] public int gridWidth;
    [ReadOnly] public bool runFullGrid;
    //[ReadOnly] public int outerMinX, outerMaxX, outerMinZ, outerMaxZ;

    // Precalculated neighbor offsets
    private static readonly int[] DX = { -1, 0, 1, -1, 1, -1, 0, 1 };
    private static readonly int[] DZ = { -1, -1, -1, 0, 0, 1, 1, 1 };
    private static readonly int[] MOVE_COSTS = { 14, 10, 14, 10, 10, 14, 10, 14 };

    public void Execute()
    {
        // Initialize target node
        int targetIndex = GetIndex(endX, endZ);
        GridNode targetCell = GridArray[targetIndex];
        targetCell.integrationCost = 0;
        GridArray[targetIndex] = targetCell;

        // Create visited array to avoid re-processing nodes
        NativeArray<bool> visited = new NativeArray<bool>(GridArray.Length, Allocator.Temp);

        // Breadth-first search
        NodeQueue.Enqueue(targetCell);
        visited[targetIndex] = true;

        while (NodeQueue.Count > 0)
        {
            GridNode currentCell = NodeQueue.Dequeue();

            for (int i = 0; i < 8; i++)
            {
                int neighborX = currentCell.x + DX[i];
                int neighborZ = currentCell.z + DZ[i];

                // Skip if outside grid bounds
                if (neighborX < 0 || neighborX >= gridWidth || neighborZ < 0 || neighborZ >= gridWidth)
                {
                    continue;
                }

                int neighborIndex = GetIndex(neighborX, neighborZ);
                GridNode neighbor = GridArray[neighborIndex];

                // Skip unwalkable or out-of-area nodes
                if (!neighbor.isWalkable || (!runFullGrid && !neighbor.isPathfindingArea))
                {
                    continue;
                }

                // Calculate new cost (including diagonal penalty)
                int moveCost = (MOVE_COSTS[i] * neighbor.cost) / 10;
                int newCost = currentCell.integrationCost + moveCost;

                if (newCost < neighbor.integrationCost)
                {
                    neighbor.integrationCost = newCost;
                    GridArray[neighborIndex] = neighbor;

                    // Only enqueue if not already visited (prevents duplicates)
                    if (!visited[neighborIndex])
                    {
                        NodeQueue.Enqueue(neighbor);
                        visited[neighborIndex] = true;
                    }
                }
            }
        }

        visited.Dispose();
    }

    private int GetIndex(int x, int z)
    {
        return z + x * gridWidth;
    }
}

[BurstCompile]
public struct WeightBuildingNodes : IJob
{
    public NativeArray<GridNode> GridArray;
    [ReadOnly] public int gridWidth;
    [ReadOnly] public int endX, endZ;

    public void Execute()
    {
        for (int index = 0; index < GridArray.Length; index++)
        {
            GridNode tempNode = GridArray[index];

            if (tempNode.isBuilding)
            {
                int x = tempNode.x;
                int z = tempNode.z;
                tempNode.integrationCost = -int.MaxValue; // Highest cost reduction for the building itself
                GridArray[index] = tempNode;

                // Expand outward in rings, updating only the perimeter
                for (int ring = 1; ring <= 10; ring++)
                {
                    int costReduction = 100000 - (ring * 100);

                    // Top and Bottom Rows
                    for (int dx = -ring; dx <= ring; dx++)
                    {
                        ProcessNode(x + dx, z - ring, costReduction); // Top row
                        ProcessNode(x + dx, z + ring, costReduction); // Bottom row
                    }

                    // Left and Right Columns (excluding corners already processed)
                    for (int dz = -ring + 1; dz <= ring - 1; dz++)
                    {
                        ProcessNode(x - ring, z + dz, costReduction); // Left column
                        ProcessNode(x + ring, z + dz, costReduction); // Right column
                    }
                }
            }
        }
    }

    private void ProcessNode(int newX, int newZ, int costReduction)
    {
        if (newX >= 0 && newX < gridWidth && newZ >= 0 && newZ < gridWidth)
        {
            int neighborIndex = GetIndex(newX, newZ);
            GridNode neighborNode = GridArray[neighborIndex];

            if (!neighborNode.isBuilding && neighborNode.isWalkable && neighborNode.isTraversable)
            {
                neighborNode.integrationCost -= costReduction;
                GridArray[neighborIndex] = neighborNode;
            }
        }
    }

    private int GetIndex(int x, int z)
    {
        return z + x * gridWidth;
    }
}



[BurstCompile]
public struct UpdateGoToIndex : IJob
{
    public NativeArray<GridNode> GridArray;
    [ReadOnly] public int gridWidth;
    [ReadOnly] public int endX, endZ;
    [ReadOnly] public bool runFullGrid;

    public void Execute()
    {
        for (int index = 0; index < GridArray.Length; index++)
        {
            GridNode tempNode = GridArray[index];

            int currentLowestIntegration = int.MaxValue;
            int currentLowestIndex = -1;

            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                for (int offsetZ = -1; offsetZ <= 1; offsetZ++)
                {
                    if (offsetX == 0 && offsetZ == 0) continue; // Skip the current node

                    int neighborX = tempNode.x + offsetX;
                    int neighborZ = tempNode.z + offsetZ;

                    // Bounds check
                    if (neighborX >= 0 && neighborX < gridWidth && neighborZ >= 0 && neighborZ < gridWidth)
                    {
                        GridNode neighbour = GridArray[GetIndex(neighborX, neighborZ)];
                        if (neighbour.integrationCost < currentLowestIntegration)
                        {
                            currentLowestIntegration = neighbour.integrationCost;
                            currentLowestIndex = neighbour.index;
                        }
                    }
                }
            }

            tempNode.goToIndex = currentLowestIndex;
            GridArray[index] = tempNode;
        }
    }

    private int GetIndex(int x, int z)
    {
        return z + x * gridWidth;
    }
}

