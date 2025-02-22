using System.IO;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;


public class NewPathfinding : MonoBehaviour
{
    [SerializeField] private MapGridManager gridManager;

    private const int MOVE_STRAIGHT_COST = 10;
    private const int MOVE_DIAGONAL_COST = 14;

    private int gridLength;
    public NativeArray<FlowGridNode> flowNodes;


    private void Awake()
    {
        gridLength = 200;
        flowNodes = new NativeArray<FlowGridNode>(gridLength * gridLength, Allocator.Persistent);
    }

    private void OnDestroy()
    {
        flowNodes.Dispose();
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
                    position = new float3(tempObject.x, 0, tempObject.z),
                    x = tempObject.x,
                    z = tempObject.z,
                    cost = 0,
                    integrationCost = 0,
                    isWalkable = tempObject.isWalkable,
                    isTraversable = tempObject.isTraversable,
                    isPathfindingArea = tempObject.isPathfindingArea,
                    goToIndex = 0
                };

                count++;
            }
        }

        RunFlowField(endWorldPosition, _runFullFlow);
    }

    public void RecalcFlowField(Vector3 endWorldPosition, bool _runFullFlow)
    {

        int count = 0;
        for (int x = 0; x < gridLength; x++)
        {
            for (int z = 0; z < gridLength; z++)
            {
                GridObject tempObject = gridManager.mapGrid.GetGridObject(x, z);

                FlowGridNode tempNode = flowNodes[count];
                tempNode.isWalkable = tempObject.isWalkable;
                tempNode.isPathfindingArea = tempObject.isPathfindingArea;
                tempNode.isBuilding = tempObject.isBuilding;
                tempNode.isTraversable = tempObject.isTraversable;
                flowNodes[count] = tempNode;

                count++;
            }
        }

        RunFlowField(endWorldPosition, _runFullFlow);
    }


    private void RunFlowField(Vector3 endWorldPosition, bool _runFullFlow)
    {
        gridManager.mapGrid.GetXZ(endWorldPosition, out int endX, out int endZ);
        
        FlowFieldScheduler flowFieldJobScheduler = new FlowFieldScheduler();
        flowFieldJobScheduler.ScheduleFlowFieldJobs(flowNodes, gridLength, endX, endZ, _runFullFlow);

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
}
