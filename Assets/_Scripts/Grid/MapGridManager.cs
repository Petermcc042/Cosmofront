using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Unity.Collections;
using Unity.Mathematics;

public class MapGridManager : MonoBehaviour
{
    public static MapGridManager Instance { get; private set; }

    [Header("GameObject References")]
    [SerializeField] private GameObject skillManagerGO;
    [SerializeField] private EnemyManager enemyManager;
    [SerializeField] private BuildableAreaMesh buildableAreaMesh;
    [SerializeField] private TerrainGen terrainGen;

    [Header("Base Buildings SO")]
    [SerializeField] private GameObject generator;

    private BuildingListSO buildingsListSO;

    [Header("Circle Points Data")]
    [SerializeField] private string circlePointsPath;
    [SerializeField] private string pathfindingCirclePointsPath;
    private List<Vector3> circlePoints;
    private List<Vector3> pathfindingCirclePoints;

    public List<float3> gridLocations;
    

    private PlacedObjectSO.Dir dir = PlacedObjectSO.Dir.Down;


    private int gridWidth = 200; //x
    private int gridLength = 200; //z

    // For Walls And Building
    private List<float3> buildableAreaList;
    private List<float3> pathfindingAreaList;
    public NativeList<float3> buildingGridSquareList;
    public NativeList<float3> traversableBuildingSquares;


    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        // Set the instance to this object
        Instance = this;

        buildableAreaList = new List<float3>();
        gridLocations = new List<float3>();
        pathfindingAreaList = new List<float3>();

        buildingGridSquareList = new NativeList<float3>(Allocator.Persistent);
        traversableBuildingSquares = new NativeList<float3>(Allocator.Persistent);

        circlePoints = ReadCSVFile(circlePointsPath);
        pathfindingCirclePoints = ReadCSVFile(pathfindingCirclePointsPath);
    }

    private void OnDestroy()
    {
        buildingGridSquareList.Dispose();
        traversableBuildingSquares.Dispose();
    }


    public void InitGrid(GameSettingsSO _gameSettings, BuildingListSO _buildingsList)
    {
        buildingsListSO = _buildingsList;
        gridWidth = _gameSettings.gridWidth;
        gridLength = _gameSettings.gridLength;

        terrainGen.InitTerrain();
        //SetPlanetObstacles();
        //SetRocks();
        //SetSmallRocks();
        SetHomeBaseArea();
    }

    public bool InBuildableArea(Vector3 _gridPos)
    {
        return buildableAreaList.Contains(_gridPos);
    }

    #region Grid Map

    private void SetRocks()
    {
        //int squareSize = 6;
        int count = 0;
        int centralSectionSize = 45;
        int numRocks = 180;

        while (count < numRocks)
        {
            bool isSevenFromEdgeX = false;
            bool isSevenFromEdgeZ = false;
            bool isNotInCentralSection = false;

            int _x = 0;
            int _z = 0;

            while (!isSevenFromEdgeX && !isSevenFromEdgeZ && !isNotInCentralSection)
            {
                _x = UnityEngine.Random.Range(7, gridWidth - 7);
                _z = UnityEngine.Random.Range(7, gridLength - 7);

                // Check if the square is 7 squares away from the edge
                isSevenFromEdgeX = _x >= 7 && _x < gridWidth - 7;
                isSevenFromEdgeZ = _z >= 7 && _z < gridLength - 7;

                // Check if the square is not in the central section
                isNotInCentralSection = _x < centralSectionSize || _x >= gridWidth - centralSectionSize ||
                                             _z < centralSectionSize || _z >= gridLength - centralSectionSize;
            }

            int gridPosX = Mathf.Clamp(_x, 0, gridWidth);
            int gridPosZ = Mathf.Clamp(_z, 0, gridLength);
            PlaceBuilding(gridPosX, gridPosZ, buildingsListSO.rockSO, false);


            count++;
        }
    }

    private void SetSmallRocks()
    {
        //int squareSize = 6;
        int count = 0;

        while (count < 500)
        {
            bool isSevenFromEdgeX = false;
            bool isSevenFromEdgeZ = false;

            int _x = 0;
            int _z = 0;

            while (!isSevenFromEdgeX && !isSevenFromEdgeZ)
            {
                _x = UnityEngine.Random.Range(7, gridWidth - 7);
                _z = UnityEngine.Random.Range(7, gridLength - 7);

                // Check if the square is 7 squares away from the edge
                isSevenFromEdgeX = _x >= 7 && _x < gridWidth - 7;
                isSevenFromEdgeZ = _z >= 7 && _z < gridLength - 7;
            }

            int gridPosX = Mathf.Clamp(_x, 0, gridWidth);
            int gridPosZ = Mathf.Clamp(_z, 0, gridLength);
            PlaceBuilding(gridPosX, gridPosZ, buildingsListSO.smallRockSO, false);

            count++;
        }
    }


    private void SetHomeBaseArea()
    {
        int radius = 8;
        int pathfindingAreaMultiplier = 3;
        int centerX = gridWidth / 2;
        int centerZ = gridLength / 2;

        for (int i = -radius; i <= radius; i++)
        {
            for (int j = -radius; j <= radius; j++)
            {
                if (i * i + j * j <= radius * radius)
                {
                    int gridPosX = centerX + i;
                    int gridPosZ = centerZ + j;

                    buildableAreaList.Add(new Vector3(gridPosX, 0, gridPosZ));
                    GridNode tempNode = PrecomputedData.GetGridObject(gridPosX, gridPosZ);
                    tempNode.isBaseArea = true;
                    PrecomputedData.SetGridNode(gridPosX, gridPosZ, tempNode);

                }
            }
        }

        // for creating flow field pathfinding
        int pathfindingRadius = radius * pathfindingAreaMultiplier;
        for (int i = -pathfindingRadius; i <= pathfindingRadius; i++)
        {
            for (int j = -pathfindingRadius; j <= pathfindingRadius; j++)
            {
                if (i * i + j * j <= pathfindingRadius * pathfindingRadius)
                {
                    int gridPosX = centerX + i;
                    int gridPosZ = centerZ + j;

                    pathfindingAreaList.Add(new Vector3(gridPosX, 0, gridPosZ));

                    GridNode tempNode = PrecomputedData.GetGridObject(gridPosX, gridPosZ);
                    tempNode.isPathfindingArea = true;
                    PrecomputedData.SetGridNode(gridPosX, gridPosZ, tempNode);
                }
            }
        }

        for (int i = -1; i <= 0; i++)
        {
            for (int j = -1; j <= 0; j++)
            {
                buildableAreaList.Remove(new Vector3(centerX + i, 0, centerZ + j));

                GridNode tempNode = PrecomputedData.GetGridObject(centerX + i, centerZ + j);
                tempNode.isBaseArea = true;
                PrecomputedData.SetGridNode(centerX + i, centerZ + j, tempNode);
            } 
        }

        MoveGenerator(buildingsListSO.generatorSO, true, generator);

        buildableAreaMesh.buildableAreas = buildableAreaList;
        buildableAreaMesh.InitMesh(false);
    }

    public void PlaceBuilding(Vector3 _mouseWorldPosition, PlacedObjectSO _placedObjectSO)
    {
        PrecomputedData.GetXZ(_mouseWorldPosition, out int x, out int z);

        if (!PrecomputedData.GetGridObject(x, z).isBaseArea) { return; }

        PlaceBuilding(x, z, _placedObjectSO, true);
    }


    private void PlaceBuilding(int _x, int _z, PlacedObjectSO _placedObjectSO, bool _location)
    {
        // return a list of grid coordinates that the current selected placedObjectSO will occupy
        // we pass in the origin and  direction (dir) to know which coordinates to search through
        List<Vector2Int> gridPosList = _placedObjectSO.GetGridPositionList(new Vector2Int(_x, _z), dir);

        // check each coordinate pulled above to see if there is a grid object on this position
        // if there is can build = false and break
        foreach (Vector2Int pos in gridPosList)
        {
            GridNode tempGrid = PrecomputedData.GetGridObject(pos.x, pos.y);

            if (!tempGrid.isBaseArea || tempGrid.isBuilding)
            {
                Debug.Log("Cannot build here - " + (!tempGrid.isBaseArea ? "Not in base area" : "Space already occupied"));
                return;
            }
        }

        foreach (Vector2Int pos in gridPosList)
        {
            if (_location)
            {
                // add to list
                buildingGridSquareList.Add(new Vector3(pos.x, 0, pos.y));
            }
        }

        // when we rotate the game object there is an offset needed to keep the object at the origin
        Vector2Int rotationOffset = _placedObjectSO.GetRotationOffset(dir);
        Vector3 placedObjectWorldPosition = PrecomputedData.GetWorldPosition(_x, _z) +
            new Vector3(rotationOffset.x, 0, rotationOffset.y) * PrecomputedData.cellSize;

        // Instantiate our gameobject and store the transform
        PlacedObject placedObject = PlacedObject.Create(placedObjectWorldPosition, new Vector2Int(_x, _z), dir, _placedObjectSO);

        // update our grid on these coordinates so that we can't build there anymore
        foreach (Vector2Int pos in gridPosList)
        {
            GridNode tempNode = PrecomputedData.GetGridObject(pos.x, pos.y);
            tempNode.isBuilding = true;
            PrecomputedData.SetGridNode(pos.x, pos.y, tempNode);
            PrecomputedData.SetPlacedObject(pos.x, pos.y, placedObject);
        }

        BuildingTraversal(_x, _z);
        enemyManager.RecalcPaths();

        if (_placedObjectSO.nameString == "Habitat Light")
        {
            PlaceCircleHabitatLight(_x, _z, false);
        }
    }

    private void BuildingTraversal(int _x, int _z)
    {
        // Start measuring time
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();

        var nodeQueue = new Queue<GridNode>();
        int searchSize = 8;

        GridNode tempGrid = PrecomputedData.GetGridObject(_x, _z);
        int minX = _x - searchSize; int maxX = _x + searchSize;
        int minZ = _z - searchSize; int maxZ = _z + searchSize;

        nodeQueue.Enqueue(tempGrid);

        while (nodeQueue.Count > 0)
        {
            GridNode currentCell = nodeQueue.Dequeue();
            if (currentCell.x < minX || currentCell.x > maxX || currentCell.z < minZ || currentCell.z > maxZ) { continue; }

            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                for (int offsetZ = -1; offsetZ <= 1; offsetZ++)
                {
                    if (offsetX == 0 && offsetZ == 0) continue; // Skip the current node

                    int neighborX = currentCell.x + offsetX;
                    int neighborZ = currentCell.z + offsetZ;

                    if (neighborX >= 0 && neighborX < gridWidth && neighborZ >= 0 && neighborZ < gridWidth)
                    {
                        GridNode neighbour = PrecomputedData.GetGridObject(neighborX, neighborZ);
                        if (!neighbour.isWalkable || neighbour.isTraversable) { continue; }

                        neighbour.isTraversable = true;
                        PrecomputedData.SetGridNode(neighborX, neighborZ, neighbour);
                        nodeQueue.Enqueue(neighbour);
                    }
                }
            }
        }

        int count = 0;
        for (int x = 0; x < gridLength; x++)
        {
            for (int z = 0; z < gridLength; z++)
            {
                GridNode tempObject = PrecomputedData.GetGridObject(x, z);
                if (tempObject.isTraversable == true)
                {
                    count++;
                }
            }
        }

        // Stop measuring time
        stopwatch.Stop();
        Debug.Log($"BFS {count}: {stopwatch.ElapsedMilliseconds} ms");
    }


    public void PlaceCircleHabitatLight(int _x, int _z, bool _bool)
    {
        foreach (var indexPos in circlePoints)
        {
            int gridX = _x + (int)indexPos.x;
            int gridZ = _z + (int)indexPos.z;
            Vector3 gridPos = new(gridX, 0, gridZ);

            if (!buildableAreaList.Contains(gridPos))
            {
                GridNode tempNode = PrecomputedData.GetGridObject(gridX, gridZ);
                tempNode.isBaseArea = true;
                PrecomputedData.SetGridNode(gridX, gridZ, tempNode);

                buildableAreaList.Add(gridPos);
            }
        }

        foreach (var indexPos in pathfindingCirclePoints)
        {
            int gridX = _x + (int)indexPos.x;
            int gridZ = _z + (int)indexPos.z;
            Vector3 gridPos = new(gridX, 0, gridZ);

            if (!pathfindingAreaList.Contains(gridPos))
            {
                GridNode tempNode = PrecomputedData.GetGridObject(gridX, gridZ);
                tempNode.isPathfindingArea = true;
                PrecomputedData.SetGridNode(gridX, gridZ, tempNode);

                pathfindingAreaList.Add(gridPos);
            }
        }

        buildableAreaMesh.buildableAreas = buildableAreaList;
        buildableAreaMesh.GenerateMesh();
    }

    public void RotateBuilding()
    {
        dir = PlacedObjectSO.GetNextDir(dir);
    }


    public void DestroyBuilding(int _x, int _z)
    {
        PlacedObject placedObject = PrecomputedData.GetPlacedObject(_x, _z);

        if (placedObject != null)
        {
            List<Vector2Int> gridPosList = placedObject.GetGridPositionList();

            foreach (Vector2Int pos in gridPosList)
            {
                PrecomputedData.DestroyBuilding(pos.x, pos.y);
                RemoveFromNativeList(buildingGridSquareList, new float3(pos.x, 0, pos.y));
            }
        }

        enemyManager.RecalcPaths();
    }

    public PlacedObjectSO.Dir GetPlacedBuildingDirection()
    {
        return dir;
    }

    #endregion


    public List<Vector3> ReadCSVFile(string _filePath)
    {
        //Debug.Log(_filePath);
        List<Vector3> vectorList = new List<Vector3>();

        try
        {
            // Read all lines from the CSV file
            string[] lines = File.ReadAllLines(_filePath.ToString());

            foreach (string line in lines)
            {
                // Split the line into individual coordinates
                string[] coordinates = line.Split(',');

                // Parse the coordinates to floats
                float x = float.Parse(coordinates[0]);
                float y = float.Parse(coordinates[1]);
                float z = float.Parse(coordinates[2]);

                // Create Vector3 and add to the list
                Vector3 vector = new Vector3(x, y, z);
                vectorList.Add(vector);
            }

            //Debug.Log("CSV file successfully read and converted to Vector3 list.");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error reading CSV file: " + e.Message);
        }

        return vectorList;
    }

    public List<Vector3> ReadCSVContent(string csvContent)
    {
        List<Vector3> vectorList = new List<Vector3>();

        try
        {
            // Split the content into lines
            string[] lines = csvContent.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                // Split the line into individual coordinates
                string[] coordinates = line.Split(',');

                // Parse the coordinates to floats
                float x = float.Parse(coordinates[0]);
                float y = float.Parse(coordinates[1]);
                float z = float.Parse(coordinates[2]);

                // Create Vector3 and add to the list
                Vector3 vector = new Vector3(x, y, z);
                vectorList.Add(vector);
            }

            // Debug.Log("CSV content successfully parsed and converted to Vector3 list.");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error parsing CSV content: " + e.Message);
        }

        return vectorList;
    }

    public void MoveGenerator(PlacedObjectSO _placedObjectSO, bool _location, GameObject generator)
    {
        int _x = (gridWidth / 2) - 1;
        int _z = (gridLength / 2) - 1;

        // return a list of grid coordinates that the current selected placedObjectSO will occupy
        // we pass in the origin and  direction (dir) to know which coordinates to search through
        List<Vector2Int> gridPosList = _placedObjectSO.GetGridPositionList(new Vector2Int(_x, _z), dir);

        // initialise if we can build
        bool canBuild = true;

        // check each coordinate pulled above to see if there is a grid object on this position
        // if there is can build = false and break
        foreach (Vector2Int pos in gridPosList)
        {
            if (PrecomputedData.GetGridObject(pos.x, pos.y).isBuilding)
            {
                canBuild = false; break;
            }
        }

        // if we can build
        if (canBuild)
        {
            foreach (Vector2Int pos in gridPosList)
            {
                DestroyBuilding(pos.x, pos.y);

                if (_location)
                {
                    buildingGridSquareList.Add(new Vector3(pos.x, 0, pos.y));
                }
            }

            // when we rotate the game object there is an offset needed to keep the object at the origin
            Vector2Int rotationOffset = _placedObjectSO.GetRotationOffset(dir);
            Vector3 placedObjectWorldPosition = PrecomputedData.GetWorldPosition(_x, _z) +
                new Vector3(rotationOffset.x, 0, rotationOffset.y) * PrecomputedData.cellSize;

            // Instantiate our gameobject and store the transform
            //PlacedObject placedObject = PlacedObject.MoveGenerator(placedObjectWorldPosition, new Vector2Int(_x, _z), dir, _placedObjectSO, generator);

            generator.transform.position = placedObjectWorldPosition;
            Vector2Int origin = new(_x, _z);

            PlacedObject genPO = generator.GetComponent<PlacedObject>();

            genPO.placedObjectSO = _placedObjectSO;
            genPO.origin = origin;
            genPO.dir = dir;

            // setting ui variables
            genPO.visibleName = _placedObjectSO.nameString;

            // update our grid on these coordinates so that we can't build there anymore
            foreach (Vector2Int pos in gridPosList)
            {
                GridNode tempNode = PrecomputedData.GetGridObject(pos.x, pos.y);
                tempNode.isBuilding = true;
                PrecomputedData.SetGridNode(pos.x, pos.y, tempNode);
                PrecomputedData.SetPlacedObject(pos.x, pos.y, genPO);
            }
        }
        else
        {
            //Debug.Log("can't build here");
        }
    }

    public static void RemoveFromNativeList(NativeList<float3> list, float3 positionToRemove)
    {
        // Find and remove the position using a swap-and-pop approach
        for (int i = list.Length - 1; i >= 0; i--)
        {
            if (list[i].Equals(positionToRemove))
            {
                list.RemoveAtSwapBack(i);
                break; // Exit once we've found and removed the position
            }
        }
    }
}