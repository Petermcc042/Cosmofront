using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;


public class NewPathfinding : MonoBehaviour
{
    [SerializeField] private MapGridManager gridManager;

    private const int MOVE_STRAIGHT_COST = 10;
    private const int MOVE_DIAGONAL_COST = 14;

    private int gridLength;

    NativeArray<GridNode> gridNodes;
    public NativeArray<FlowGridNode> flowNodes;


    private void Awake()
    {
        gridLength = 200;
        gridNodes = new NativeArray<GridNode>(gridLength * gridLength, Allocator.Persistent);
        flowNodes = new NativeArray<FlowGridNode>(gridLength * gridLength, Allocator.Persistent);
    }

    private void OnDestroy()
    {
        gridNodes.Dispose();
        flowNodes.Dispose();
    }

    public List<Vector3> FindPath(Vector3 startWorldPosition, Vector3 endWorldPosition)
    {
        int count = 0;
        for (int x = 0; x < gridLength; x++)
        {
            for (int z = 0; z < gridLength; z++)
            {
                GridObject tempObject = gridManager.mapGrid.GetGridObject(x, z);
                gridNodes[count] = new GridNode()
                {
                    index = count,
                    x = tempObject.x,
                    z = tempObject.z,
                    gCost = tempObject.gCost,
                    hCost = tempObject.hCost,
                    fCost = tempObject.fCost,
                    dCost = tempObject.dCost,
                    isWalkable = tempObject.isWalkable,
                    cameFromIndex = 0
                };

                flowNodes[count] = new FlowGridNode()
                {
                    index = count,
                    x = tempObject.x,
                    z = tempObject.z,
                    cost = 0,
                    integrationCost = 0,
                    isWalkable = tempObject.isWalkable,
                    goToIndex = 0
                };

                count++;
            }
        }

        gridManager.mapGrid.GetXZ(startWorldPosition, out int startX, out int startZ);
        gridManager.mapGrid.GetXZ(endWorldPosition, out int endX, out int endZ);


        List<Vector3> path = FindPathUsingJobs(startX, startZ, endX, endZ);
        //RunFlowField(endX, endZ);

        if (path == null)
        {
            return null;
        }
        else
        {
            return path;
        }
    }

    public void StartFlowField(Vector3 endWorldPosition, bool _runFullFlow)
    {
        int count = 0;
        for (int x = 0; x < gridLength; x++)
        {
            for (int z = 0; z < gridLength; z++)
            {
                GridObject tempObject = gridManager.mapGrid.GetGridObject(x, z);

                flowNodes[count] = new FlowGridNode()
                {
                    index = count,
                    x = tempObject.x,
                    z = tempObject.z,
                    cost = 0,
                    integrationCost = 0,
                    isWalkable = tempObject.isWalkable,
                    isPathfindingArea = tempObject.isPathfindingArea,
                    goToIndex = 0
                };

                count++;
            }
        }

        RunFlowField(endWorldPosition, _runFullFlow);
    }

    public void StartPartialFlowField(Vector3 endWorldPosition, bool _runFullFlow)
    {
        int count = 0;
        for (int x = 0; x < gridLength; x++)
        {
            for (int z = 0; z < gridLength; z++)
            {
                GridObject tempObject = gridManager.mapGrid.GetGridObject(x, z);

                FlowGridNode tempNode = flowNodes[count]; 

                tempNode.isWalkable = tempObject.isWalkable;
                tempNode.isPathfindingArea = !tempObject.isBaseArea;

                flowNodes[count] = tempNode;

                count++;
            }
        }

        RunFlowField(endWorldPosition, _runFullFlow);
    }


    private void RunFlowField(Vector3 endWorldPosition, bool _runFullFlow)
    {
        gridManager.mapGrid.GetXZ(endWorldPosition, out int endX, out int endZ);
        // we are passing the correct end position
        

        UpdateNodesMovementCost flowJob1 = new UpdateNodesMovementCost
        {
            GridArray = flowNodes,
            endX = endX, endZ = endZ,
            runFullGrid = _runFullFlow
        };

        JobHandle flowHandle1 = flowJob1.Schedule(flowNodes.Length, 64);
        flowHandle1.Complete();


        NativeQueue<FlowGridNode> nodeQueue = new NativeQueue<FlowGridNode>(Allocator.Persistent);

        UpdateNodesIntegration flowJob2 = new UpdateNodesIntegration
        {
            GridArray = flowNodes,
            NodeQueue = nodeQueue,
            endX = endX,
            endZ = endZ,
            gridWidth = gridLength,
            runFullGrid = _runFullFlow
        };

        JobHandle flowHandle2 = flowJob2.Schedule();
        flowHandle2.Complete();

        nodeQueue.Dispose();


        UpdateGoToIndex flowJob3 = new UpdateGoToIndex
        {
            GridArray = flowNodes,
            endX = endX,
            endZ = endZ,
            runFullGrid = _runFullFlow,
            gridWidth = gridLength
        };

        JobHandle flowHandle3 = flowJob3.Schedule();
        flowHandle3.Complete();


        WriteDataToCSV("output.csv");
    }

    public void WriteDataToCSV(string filePath)
    {
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            // Write the header
            writer.WriteLine("index,GoToIndex,integrationCost,GoFrom,GoTo");

            // Write each flowNode's data
            for (int i = 0; i < flowNodes.Length; i++)
            {
                if (flowNodes[i].goToIndex<0)
                {
                    string line = $"{flowNodes[i].index},{flowNodes[i].goToIndex},{flowNodes[i].integrationCost},{flowNodes[i].x}:{flowNodes[i].z},0";
                    writer.WriteLine(line);
                } else
                {
                    string line = $"{flowNodes[i].index},{flowNodes[i].goToIndex},{flowNodes[i].integrationCost},{flowNodes[i].x}:{flowNodes[i].z},{flowNodes[flowNodes[i].goToIndex].x}:{flowNodes[flowNodes[i].goToIndex].z}";
                    writer.WriteLine(line);
                }
                
            }
        }
    }

    private List<Vector3> FindPathUsingJobs(int startX, int startZ, int endX, int endZ)
    {
        NativeList<int2> path = new NativeList<int2>(Allocator.TempJob);
        NativeList<FixedString32Bytes> debugList = new NativeList<FixedString32Bytes>(Allocator.TempJob);

        PathfindingJob job = new PathfindingJob
        {
            startX = startX,
            startZ = startZ,
            endX = endX,
            endZ = endZ,
            gridWidth = gridLength,
            grid = gridNodes,
            path = path,
            debugList = debugList
        };

        JobHandle handle = job.Schedule();
        handle.Complete();

        for (int i = 0; i < debugList.Length; i++)
        {
            //Debug.Log(debugList[i] + " - Start pos: " + startX + ":" + startZ);
            //Debug.Log(debugList[i] + " - End pos: " + endX + ":" + endZ);
            Debug.Log(debugList[i]);
        }

        // Convert the NativeList to a usable list of positions
        List<Vector3> worldPath = new List<Vector3>();
        foreach (int2 pos in path)
        {
            worldPath.Add(new Vector3(pos.x, 0, pos.y));
        }

        path.Dispose();

        return worldPath;
    }

}
