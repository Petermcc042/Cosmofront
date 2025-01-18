using System.Diagnostics;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

public struct GridNode
{
    public int index;
    public int x;
    public int z;
    public int gCost;
    public int hCost;
    public int fCost;
    public int dCost;
    public bool isWalkable;
    public int cameFromIndex; // Index of the parent node for path reconstruction
}


public struct PathfindingJob : IJob
{
    public int startX, startZ;
    public int endX, endZ;
    public int gridWidth; // width = 200

    public NativeArray<GridNode> grid;
    public NativeList<int2> path;
    public NativeList<FixedString32Bytes> debugList;


    public void Execute()
    {
        // Initialize local open and closed lists
        NativeList<GridNode> openList = new NativeList<GridNode>(Allocator.Temp);
        NativeList<GridNode> closedList = new NativeList<GridNode>(Allocator.Temp);

        // Start node index
        GridNode startNode = GetGridNode(startX, startZ);
        GridNode endNode = GetGridNode(endX, endZ);

        // this is just setup for the algorithm
        // Set the default values for all grid objects
        for (int x = 0; x < gridWidth * gridWidth; x++)
        {
            GridNode tempNode = grid[x];
            tempNode.gCost = int.MaxValue;
            tempNode.fCost = tempNode.gCost + tempNode.hCost + tempNode.dCost;
            tempNode.cameFromIndex = 0;
            grid[x] = tempNode;
        }

        //debugList.Add("Start pos: " + grid[startIndex].x + ":" + grid[startIndex].z);
        //debugList.Add("End pos: " + grid[endIndex].x + ":" + grid[endIndex].z);

        // Setup the start node
        startNode.gCost = 0;
        startNode.hCost = CalculateDistanceCost(startX, startZ, endX, endZ);
        startNode.fCost = startNode.gCost + startNode.hCost;
        startNode.cameFromIndex = -1;
        grid[GetIndex(startX, startZ)] = startNode;

        openList.Add(startNode);

        // This is the real algorithm before is just set-up
        // while there are nodes still to search
        while (openList.Length > 0)
        {
            // Find node with the lowest fCost
            GridNode currentNode = GetLowestFCost(openList);
            //debugList.Add("Current: " + currentNode.x + ":" + currentNode.z + " > " + endNode.x + ":" + endNode.z);
            //debugList.Add(currentNode.x + ":" + currentNode.z + " > " + currentNode.cameFromIndex + ":" + grid[currentNode.cameFromIndex].x + ":" + grid[currentNode.cameFromIndex].z);

            // Check if we reached the end
            if (currentNode.x == endNode.x && currentNode.z == endNode.z)
            {
                // Reconstruct the path
                debugList.Add("made it to the end");
                //debugList.Add("Current: " + currentNode.x + ":" + currentNode.z + " > " + endNode.x + ":" + endNode.z);
                ReconstructPath(currentNode);
                break;
            }

            // Move from open to closed
            int indexToRemove = -1;
            for (int i = 0; i < openList.Length; i++)
            {
                if (openList[i].index == currentNode.index)
                {
                    indexToRemove = i;
                    break;
                }
            }

            if (indexToRemove >= 0)
            {
                openList.RemoveAt(indexToRemove);
            }

            closedList.Add(currentNode);

            // Process neighbors
            NativeList<GridNode> neighbourList = GetNeighborNodes(currentNode.x, currentNode.z);

            for (int i = 0; i < neighbourList.Length; i++) 
            {
                GridNode neighborNode = neighbourList[i];
                
                if (ListContains(closedList, neighborNode)) continue;

                if (!neighborNode.isWalkable) continue;

                // Calculate tentative gCost
                int tentativeGCost = currentNode.gCost + CalculateDistanceCost(currentNode.x, currentNode.z, neighborNode.x, neighborNode.z);

                if (tentativeGCost < neighborNode.gCost)
                {
                    neighborNode.cameFromIndex = currentNode.index;
                    neighborNode.gCost = tentativeGCost;
                    neighborNode.hCost = CalculateDistanceCost(neighborNode.x, neighborNode.z, endX, endZ);
                    neighborNode.fCost = neighborNode.gCost + neighborNode.hCost;

                    grid[neighborNode.index] = neighborNode;

                    if (!ListContains(openList, neighborNode))
                    {
                        openList.Add(neighborNode);
                    }
                }
            }

            neighbourList.Dispose();
        }

        openList.Dispose();
        closedList.Dispose();
    }

    private void ReconstructPath(GridNode endNode)
    {
        path.Clear();
        GridNode currentNode = endNode;

        while (currentNode.cameFromIndex != -1)
        {
            path.Add(new int2(currentNode.x, currentNode.z));
            currentNode = grid[currentNode.cameFromIndex];
        }

        // Add the start node to the path
        path.Add(new int2(currentNode.x, currentNode.z));

        // Reverse the path since we constructed it from end to start
        path.Reverse();
    }

    private int GetIndex(int x, int z)
    {
        return z + x * gridWidth;
    }

    private GridNode GetGridNode(int x, int z)
    {
        return grid[GetIndex(x, z)];
    }

    private bool ListContains(NativeList<GridNode> list, GridNode node)
    {
        for (int i = 0; i < list.Length; i++)
        {
            if (list[i].index == node.index) // Compare based on unique `index`
            {
                return true;
            }
        }
        return false;
    }

    private int CalculateDistanceCost(int x1, int z1, int x2, int z2)
    {
        int dx = math.abs(x1 - x2);
        int dz = math.abs(z1 - z2);
        int remaining = math.abs(dx - dz);
        return 14 * math.min(dx, dz) + 10 * remaining;
    }

    private NativeList<GridNode> GetNeighborNodes(int x, int z)
    {
        NativeList<GridNode> neighbors = new NativeList<GridNode>(Allocator.Temp);

        // Cardinal neighbors
        TryAddNeighbor(x - 1, z, neighbors); // Left
        TryAddNeighbor(x + 1, z, neighbors); // Right
        TryAddNeighbor(x, z - 1, neighbors); // Down
        TryAddNeighbor(x, z + 1, neighbors); // Up

        // Diagonal neighbors
        TryAddNeighbor(x - 1, z - 1, neighbors); // Bottom-left
        TryAddNeighbor(x + 1, z - 1, neighbors); // Bottom-right
        TryAddNeighbor(x - 1, z + 1, neighbors); // Top-left
        TryAddNeighbor(x + 1, z + 1, neighbors); // Top-right

        return neighbors;
    }


    private void TryAddNeighbor(int x, int z, NativeList<GridNode> neighbors)
    {
        if (x >= 0 && x < gridWidth && z >= 0 && z < gridWidth) // Bounds check
        {
            neighbors.Add(GetGridNode(x, z));
        }
    }

    // linear search more nodes means worse performance
    private GridNode GetLowestFCost(NativeList<GridNode> _pathNodeList)
    {
        GridNode lowestFCostNode = _pathNodeList[0];
        for (int i = 1; i < _pathNodeList.Length; i++)
        {
            if (_pathNodeList[i].fCost < lowestFCostNode.fCost)
            {
                lowestFCostNode = _pathNodeList[i];
            }
        }
        return lowestFCostNode;
    }
}
