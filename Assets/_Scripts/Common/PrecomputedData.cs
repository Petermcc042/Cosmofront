using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public static class PrecomputedData
{
    public static void Clear()
    {
        creationNeeded = true;
        length = 0;
        cellSize = 1;
        originPosition = Vector3.zero;
        if (gridArray.IsCreated) gridArray.Dispose();
    }

    public static bool creationNeeded = true;

    private static int length = 200;
    public static float cellSize = 1;
    private static Vector3 originPosition = Vector3.zero;
    public static NativeArray<GridNode> gridArray;
    private static PlacedObject[] placedObjectArray;

    public static void InitGrid(int _length)
    {
        length = _length;

        gridArray = new NativeArray<GridNode>(length * length, Allocator.Persistent);
        placedObjectArray = new PlacedObject[length * length];

        CreateGrid();
        RunFlowFieldJobs(99,99,true);
    }

    public static void CreateGrid() {
        int count = 0;
        for (int x = 0; x < length; x++)
        {
            for (int z = 0; z < length; z++)
            {
                GridNode tempNode = new() { x = x, z = z };

                gridArray[count] = tempNode;
                count++;
            }
        }
    }

    public static Vector3 GetWorldPosition(int x, int z)
    {
        return new Vector3(x,0,z) * cellSize + originPosition;
    }

    public static void GetXZ(Vector3 worldPosition, out int x, out int z)
    {
        x = Mathf.FloorToInt((worldPosition - originPosition).x / cellSize);
        z = Mathf.FloorToInt((worldPosition - originPosition).z / cellSize);
    }

    public static GridNode GetGridObject(Vector3 worldPosition)
    {
        GetXZ(worldPosition, out int x, out int z);
        return GetGridObject(x, z);
    }

    public static GridNode GetGridObject(int x, int z)
    {
        if (x >= 0 && z >= 0 && x < length && z < length)
        {
            return gridArray[GetIndex(x,z)];
        }
        else
        {
            return default(GridNode);
        }
    }

    public static void SetGridNode(int x, int z, GridNode _flowGridNode)
    {
        gridArray[GetIndex(x,z)] = _flowGridNode;
    }

    public static PlacedObject GetPlacedObject(Vector3 worldPosition)
    {
        GetXZ(worldPosition, out int x, out int z);
        return GetPlacedObject(x, z);
    }
    
    public static PlacedObject GetPlacedObject(int x, int z)
    {
        if (x >= 0 && z >= 0 && x < length && z < length)
        {
            return placedObjectArray[GetIndex(x,z)];
        }
        else
        {
            return default(PlacedObject);
        }
    }

    public static void SetPlacedObject(int x, int z, PlacedObject _placedObject)
    {
        placedObjectArray[GetIndex(x,z)] = _placedObject;
    }

    public static void DestroyBuilding(int _x, int _z)
    {
        placedObjectArray[GetIndex(_x,_z)].DestroySelf();
        placedObjectArray[GetIndex(_x,_z)] = null;

        GridNode tempNode = GetGridObject(_x,_z);
        tempNode.isBuilding = false;
        SetGridNode(_x,_z, tempNode);
    }

    private static int GetIndex(int x, int z)
    {
        return z + x * length;
    }

    private static void RunFlowFieldJobs(int endX, int endZ, bool runFullFlow) {
        
        var updateCostJob = new UpdateNodesMovementCost
        {
            GridArray = gridArray,
            endX = endX,
            endZ = endZ,
            runFullGrid = runFullFlow
        };
        JobHandle handle1 = updateCostJob.Schedule(gridArray.Length, 64);

        var nodeQueue = new NativeQueue<GridNode>(Allocator.TempJob);
        
        try 
        {
            var updateIntegrationJob = new UpdateNodesIntegration
            {
                GridArray = gridArray,
                NodeQueue = nodeQueue,
                endX = endX,
                endZ = endZ,
                gridWidth = length,
                runFullGrid = runFullFlow
            };
            JobHandle handle2 = updateIntegrationJob.Schedule(handle1);


            WeightBuildingNodes flowJob3 = new WeightBuildingNodes
            {
                GridArray = gridArray,
                endX = endX,
                endZ = endZ,
                gridWidth = length
            };

            JobHandle handle3 = flowJob3.Schedule(handle2);


            UpdateGoToIndex flowJob4 = new UpdateGoToIndex
            {
                GridArray = gridArray,
                endX = endX,
                endZ = endZ,
                runFullGrid = runFullFlow,
                gridWidth = length
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