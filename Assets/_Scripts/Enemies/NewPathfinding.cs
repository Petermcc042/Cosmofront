using System.Collections;
using System.Collections.Generic;
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


    private void Awake()
    {
        gridLength = 200;
        gridNodes = new NativeArray<GridNode>(gridLength * gridLength, Allocator.Persistent);
    }

    private void OnDestroy()
    {
        gridNodes.Dispose();
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

                count++;
            }
        }

        gridManager.mapGrid.GetXZ(startWorldPosition, out int startX, out int startZ);
        gridManager.mapGrid.GetXZ(endWorldPosition, out int endX, out int endZ);


        List<Vector3> path = FindPathUsingJobs(startX, startZ, endX, endZ);

        if (path == null)
        {
            return null;
        }
        else
        {
            return path;
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
