using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;


public class Pathfinding : MonoBehaviour
{    
    public static Pathfinding Instance { get; private set; } // don't forget to set the instance in Awake

    [SerializeField] MapGridManager gridManager;

    [SerializeField] private bool debugLinesBool;


    private const int MOVE_STRAIGHT_COST = 10;
    private const int MOVE_DIAGONAL_COST = 14;

    private int gridLength;


    private List<GridObject> openList;
    private List<GridObject> closedList;

    private void Awake()
    {
        // Set the instance to this object
        Instance = this;
        openList = new List<GridObject>();
        closedList = new List<GridObject>();
        gridLength = 200;
    }


    public void StartPathfind(Vector3 _mouseWorldPosition)
    {
        gridManager.mapGrid.GetXZ(_mouseWorldPosition, out int _x, out int _z);

        List<GridObject> path = FindPath(0, 0, _x, _z);

        if (debugLinesBool)
        {
            if (path != null)
            {
                for (int i = 0; i < path.Count - 1; i++)
                {
                    //Debug.Log(path[i].x + ":" + path[i].z + " -> " + path[i+1].x + ":" + path[i+1].z);
                    Debug.DrawLine(new Vector3(path[i].x, 1, path[i].z) + Vector3.one * 0.5f, new Vector3(path[i + 1].x, 1, path[i + 1].z) + Vector3.one * 0.5f, UnityEngine.Color.black, 100f);
                }
            }
        }


    }

    public List<Vector3> FindPath(Vector3 startWorldPosition, Vector3 endWorldPosition)
    {
        gridManager.mapGrid.GetXZ(startWorldPosition, out int startX, out int startZ);
        gridManager.mapGrid.GetXZ(endWorldPosition, out int endX, out int endZ);

        //Debug.Log("The start node is at:" + startX + ":"+ startZ);
        //Debug.Log("The end node is at:" + endX + ":" + endZ);

        List<GridObject> path = FindPath(startX, startZ, endX, endZ);

        if (path == null)
        {
            return null;
        }
        else
        {
            List<Vector3> vectorPath = new List<Vector3>();
            foreach (GridObject pathNode in path)
            {
                vectorPath.Add(new Vector3(pathNode.x, 0, pathNode.z));
            }
            return vectorPath;
        }
    }

    public List<GridObject> FindPath(int startX, int startZ, int endX, int endZ)
    {
        GridObject startNode = gridManager.mapGrid.GetGridObject(startX, startZ);
        GridObject endNode = gridManager.mapGrid.GetGridObject(endX, endZ);

        openList.Clear();
        closedList.Clear();

        openList.Add(startNode);

        // this is just setup for the algorithm
        // Set the default values for all grid objects
        for (int x = 0; x < gridLength; x++)
        {
            for (int z = 0; z < gridLength; z++)
            {
                GridObject pathNode = gridManager.mapGrid.GetGridObject(x, z);
                pathNode.gCost = int.MaxValue;
                pathNode.CalculateFCost();
                pathNode.cameFromNode = null;
            }
        }

        // calculate the shortest distance to the end node from the start
        startNode.gCost = 0;
        startNode.hCost = CalculateDistanceCost(startNode, endNode);
        startNode.CalculateFCost();

        // This is the real algorithm before is just set-up
        // while there are nodes still to search
        while (openList.Count > 0)
        {
            // start with the lowest FCost (start node initially)
            GridObject currentNode = GetLowestFCost(openList);
            //Debug.Log(currentNode.x + ":" + currentNode.z + " looking for " + endNode.x + ":" + endNode.z);

            if (currentNode == endNode)
            {
                return CalculatePath(endNode);
            }

            // We have checked and this is not the end node so remove from the open list and add to closed
            openList.Remove(currentNode);
            closedList.Add(currentNode);

            // now check each of the nodes neighbours to see if it is the end
            foreach (GridObject neighbourNode in GetNeighboursList(currentNode))
            {
                if (closedList.Contains(neighbourNode)) continue;

                // check if there is a placed object on this grid space
                // if so skip it as a potential and add to the closed list
                if (!neighbourNode.isWalkable)
                {
                    //Debug.Log(neighbourNode.x + ":" + neighbourNode.z + ":" + neighbourNode.IsWalkable());
                    closedList.Add(neighbourNode);
                    continue;
                }

                // if the node has not already been checked and there is nothing on it 
                // work out if it is right 
                int tentativeGCost = currentNode.gCost + CalculateDistanceCost(currentNode, neighbourNode);
                if (tentativeGCost < neighbourNode.gCost)
                {
                    neighbourNode.cameFromNode = currentNode;
                    neighbourNode.gCost = tentativeGCost;
                    neighbourNode.hCost = CalculateDistanceCost(neighbourNode, endNode);
                    neighbourNode.CalculateFCost();
                }

                if (!openList.Contains(neighbourNode))
                {
                    openList.Add(neighbourNode);
                }
            }
        }

        // Out of nodes on the openList
        Debug.Log("no available path");
        return null;
    }


    private List<GridObject> GetNeighboursList(GridObject currentNode)
    {
        List<GridObject> neighbourList = new List<GridObject>();

        int x = currentNode.x;
        int z = currentNode.z;

        // Left
        if (x - 1 >= 0) neighbourList.Add(GetNode(x - 1, z));
        // Right
        if (x + 1 < gridLength) neighbourList.Add(GetNode(x + 1, z));
        // Down
        if (z - 1 >= 0) neighbourList.Add(GetNode(x, z - 1));
        // Up
        if (z + 1 < gridLength) neighbourList.Add(GetNode(x, z + 1));

        // Diagonals
        if (x - 1 >= 0 && z - 1 >= 0) neighbourList.Add(GetNode(x - 1, z - 1)); // Left-Down
        if (x - 1 >= 0 && z + 1 < gridLength) neighbourList.Add(GetNode(x - 1, z + 1)); // Left-Up
        if (x + 1 < gridLength && z - 1 >= 0) neighbourList.Add(GetNode(x + 1, z - 1)); // Right-Down
        if (x + 1 < gridLength && z + 1 < gridLength) neighbourList.Add(GetNode(x + 1, z + 1)); // Right-Up

        return neighbourList;
    }

    private GridObject GetNode(int x, int z)
    {
        return gridManager.mapGrid.GetGridObject(x, z);
    }

    private List<GridObject> CalculatePath(GridObject endNode)
    {
        Stack<GridObject> pathStack = new Stack<GridObject>();
        GridObject currentNode = endNode;

        // Add the end node to the path first
        pathStack.Push(currentNode);

        int addCostCount = 0;

        // Walk backward from the end node to the start
        while (currentNode.cameFromNode != null)
        {
            if (addCostCount > 3) { currentNode.dCost += 10; }
            currentNode = currentNode.cameFromNode;
            pathStack.Push(currentNode);  // Add each node to the path
            addCostCount++;
        }

        return pathStack.ToList();  // Convert stack to list for the final path
    }

    private int CalculateDistanceCost(GridObject _a, GridObject _b)
    {
        //Debug.Log("The start node is at:" + _a.x + ":" + _a.z);
        //Debug.Log("The end node is at:" + _b.x + ":" + _b.z);
        int xDistance = Mathf.Abs(_a.x - _b.x);
        int yDistance = Mathf.Abs(_a.z - _b.z);
        int remaining = Mathf.Abs(xDistance - yDistance);
        return MOVE_DIAGONAL_COST * Mathf.Min(xDistance, yDistance) + MOVE_STRAIGHT_COST * remaining;
    }

    // linear search more nodes means worse performance
    private GridObject GetLowestFCost(List<GridObject> _pathNodeList)
    {
        GridObject lowestFCostNode = _pathNodeList[0];
        for (int i = 1; i < _pathNodeList.Count; i++)
        {
            if (_pathNodeList[i].fCost < lowestFCostNode.fCost)
            {
                lowestFCostNode = _pathNodeList[i];
            }
        }
        return lowestFCostNode;
    }

}