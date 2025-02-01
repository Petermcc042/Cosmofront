using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class buildGrid : MonoBehaviour
{
/*    private const int MOVE_STRAIGHT_COST = 10;
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
    }

    public void StartFlowField(Vector3 endWorldPosition, bool _runFullFlow)
    {
        int count = 0;
        for (int x = 0; x < gridLength; x++)
        {
            for (int z = 0; z < gridLength; z++)
            {
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
    }*/
}
