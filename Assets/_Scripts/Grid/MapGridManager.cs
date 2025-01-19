using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Unity.Collections;

public class MapGridManager : MonoBehaviour
{
    public static MapGridManager Instance { get; private set; }

    [Header("GameObject References")]
    [SerializeField] private GameObject skillManagerGO;
    [SerializeField] private EnemyManager enemyManager;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private WallManager wallManager;
    [SerializeField] private CollisionManager collisionManager;
    [SerializeField] private BuildableAreaMesh buildableAreaMesh;
    [SerializeField] private TerrainGen terrainGen;

    [Header("Base Buildings SO")]
    [SerializeField] private GameObject generator;

    [Header("Environment PlacedObjectSO")]
    [SerializeField] private GameObject groundGameObjects;
    [SerializeField] private GameObject groundObject;
    [SerializeField] private GameObject gridObjectTile;

    private BuildingListSO buildingsListSO;

    [Header("Circle Points Data")]
    [SerializeField] private string radiusPointsPath;
    [SerializeField] private string innerPointsPath;
    private List<Vector3> radiusPoints;
    private List<Vector3> innerPoints;
    private List<Vector3> newDiagPoints;

    [SerializeField] private bool debugLinesBool;

    public GridXZ<GridObject> mapGrid;
    public List<Vector3> gridLocations;

    private PlacedObjectSO.Dir dir = PlacedObjectSO.Dir.Down;

    public event EventHandler<BuildingAddedEventArgs> BuildingAddedEvent;
    public class BuildingAddedEventArgs : EventArgs { public Vector3 coord; public bool remove; public bool shield = false; }


    private int gridWidth = 200; //x
    private int gridLength = 200; //z
    private float cellSize = 1f;

    // For Walls And Building
    private List<Vector3> buildableAreaList;


    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        // Set the instance to this object
        Instance = this;

        gridLocations = new List<Vector3>();

        radiusPoints = ReadCSVFile(radiusPointsPath);
        innerPoints = ReadCSVFile(innerPointsPath);
    }


    public void InitGrid(GameSettingsSO _gameSettings, BuildingListSO _buildingsList)
    {
        buildingsListSO = _buildingsList;
        gridWidth = _gameSettings.gridWidth;
        gridLength = _gameSettings.gridLength;

        mapGrid = new GridXZ<GridObject>(gridWidth, gridLength, cellSize, Vector3.zero, (GridXZ<GridObject> g, int x, int z) => new GridObject(g, x, z));

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
        int centerX = gridWidth / 2;
        int centerZ = gridLength / 2;

        buildableAreaList = new List<Vector3>();

        for (int i = -radius; i <= radius; i++)
        {
            for (int j = -radius; j <= radius; j++)
            {
                if (i * i + j * j <= radius * radius)
                {
                    int gridPosX = centerX + i;
                    int gridPosZ = centerZ + j;

                    buildableAreaList.Add(new Vector3(gridPosX, 0, gridPosZ));
                    mapGrid.GetGridObject(gridPosX, gridPosZ).isBaseArea = true;
                }
            }
        }

        for (int i = -1; i <= 0; i++)
        {
            for (int j = -1; j <= 0; j++)
            {
                buildableAreaList.Remove(new Vector3(centerX + i, 0, centerZ + j));
                mapGrid.GetGridObject(centerX + i, centerZ + j).isBaseArea = false;
            } 
        }

        MoveGenerator(buildingsListSO.generatorSO, true, generator);

        buildableAreaMesh.buildableAreas = buildableAreaList;
        buildableAreaMesh.InitMesh(false);
    }

    public void PlaceHabitatLight(Vector3 _mouseWorldPosition, bool _bool)
    {
        mapGrid.GetXZ(_mouseWorldPosition, out int x, out int z);
        PlaceCircleHabitatLight(x, z, _bool);
    }

    public void PlaceCircleHabitatLight(int _x, int _z, bool _bool)
    {
        GridObject tempGridObject = mapGrid.GetGridObject(_x, _z);

        if (!tempGridObject.isBaseArea) { return; }

        if (tempGridObject.isBuilding) { return; }

        PlaceBuilding(_x, _z, buildingsListSO.habitatLightSO, true);

        // on the radius at some mark
        foreach (var indexPos in radiusPoints)
        {
            int gridX = _x + (int)indexPos.x;
            int gridZ = _z + (int)indexPos.z;
            Vector3 gridPos = new(gridX, 0, gridZ);

            if (!buildableAreaList.Contains(gridPos))
            {
                mapGrid.GetGridObject(gridX, gridZ).isBaseArea = true;
                buildableAreaList.Add(gridPos);
            }
        }

        foreach (var indexPos in innerPoints)
        {
            int gridX = _x + (int)indexPos.x;
            int gridZ = _z + (int)indexPos.z;
            Vector3 gridPos = new(gridX, 0, gridZ);

            if (!buildableAreaList.Contains(gridPos))
            {
                // outside of buildable area place walls on circum
                // add all squares to buildable area
                mapGrid.GetGridObject(gridX, gridZ).isBaseArea = true;
                buildableAreaList.Add(gridPos);
            }
        }

        buildableAreaMesh.buildableAreas = buildableAreaList;
        buildableAreaMesh.GenerateMesh();
    }

    public void RotateBuilding()
    {
        dir = PlacedObjectSO.GetNextDir(dir);
    }


    public void PlaceBuilding(Vector3 _mouseWorldPosition, PlacedObjectSO _placedObjectSO)
    {
        mapGrid.GetXZ(_mouseWorldPosition, out int x, out int z);
        //Debug.Log("coord" + x + ":" + z);

        if (!mapGrid.GetGridObject(x, z).isBaseArea) { return; }

        if (_placedObjectSO.nameString == "Habitat Light")
        {
            PlaceHabitatLight(_mouseWorldPosition, false);
        }
        else
        {
            PlaceBuilding(x, z, _placedObjectSO, true);
        }
    }


    private void PlaceBuilding(int _x, int _z, PlacedObjectSO _placedObjectSO, bool _location)
    {
        // return a list of grid coordinates that the current selected placedObjectSO will occupy
        // we pass in the origin and  direction (dir) to know which coordinates to search through
        List<Vector2Int> gridPosList = _placedObjectSO.GetGridPositionList(new Vector2Int(_x, _z), dir);
        //Debug.Log(gridPosList.Count);

        // initialise if we can build
        bool canBuild = true;
        // check each coordinate pulled above to see if there is a grid object on this position
        // if there is can build = false and break
        foreach (Vector2Int pos in gridPosList)
        {
            if (mapGrid.GetGridObject(pos.x, pos.y).isBuilding)
            {
                canBuild = false; break;
            }
        }

        // if we can build
        if (canBuild)
        {
            foreach (Vector2Int pos in gridPosList)
            {
                if (_location)
                {
                    BuildingAddedEvent?.Invoke(this, new BuildingAddedEventArgs { coord = new Vector3(pos.x, 0, pos.y), remove = false});
                }
            }

            // when we rotate the game object there is an offset needed to keep the object at the origin
            Vector2Int rotationOffset = _placedObjectSO.GetRotationOffset(dir);
            Vector3 placedObjectWorldPosition = mapGrid.GetWorldPosition(_x, _z) +
                new Vector3(rotationOffset.x, 0, rotationOffset.y) * mapGrid.GetCellSize();

            // Instantiate our gameobject and store the transform
            PlacedObject placedObject = PlacedObject.Create(placedObjectWorldPosition, new Vector2Int(_x, _z), dir, _placedObjectSO);

            // update our grid on these coordinates so that we can't build there anymore
            foreach (Vector2Int pos in gridPosList)
            {
                GridObject tempObject = mapGrid.GetGridObject(pos.x, pos.y);
                tempObject.isBuilding = true;
                tempObject.SetPlacedObject(placedObject);
                tempObject.dCost += 20;
            }

        }
        else
        {
            //Debug.Log("can't build here");
        }
    }

    public void DestroyBuilding(Vector3 _mouseWorldPosition)
    {
        mapGrid.GetXZ(_mouseWorldPosition, out int _x, out int _z);
        DestroyBuilding(_x, _z);
    }

    public void DestroyBuilding(int _x, int _z)
    {
        GridObject gridObject = mapGrid.GetGridObject(_x, _z);
        PlacedObject placedObject = gridObject.GetPlacedObject();
        if (placedObject != null)
        {
            placedObject.DestroySelf();


            List<Vector2Int> gridPosList = placedObject.GetGridPositionList();

            foreach (Vector2Int pos in gridPosList)
            {
                GridObject tempObject = mapGrid.GetGridObject(pos.x, pos.y);
                tempObject.isBuilding = true;
                tempObject.ClearPlacedObject();

                BuildingAddedEvent?.Invoke(this, new BuildingAddedEventArgs { coord = new Vector3(pos.x, 0, pos.y), remove = true });
            }
        }
    }

    public void DestroyBuilding(PlacedObject _placedObject)
    {
        if (_placedObject != null)
        {
            _placedObject.DestroySelf();

            List<Vector2Int> gridPosList = _placedObject.GetGridPositionList();

            foreach (Vector2Int pos in gridPosList)
            {
                GridObject tempObject = mapGrid.GetGridObject(pos.x, pos.y);
                tempObject.isBuilding = true;
                tempObject.ClearPlacedObject();

                BuildingAddedEvent?.Invoke(this, new BuildingAddedEventArgs { coord = new Vector3(pos.x, 0, pos.y), remove = true });
            }
        }
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
            if (mapGrid.GetGridObject(pos.x, pos.y).isBuilding)
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
                    BuildingAddedEvent?.Invoke(this, new BuildingAddedEventArgs { coord = new Vector3(pos.x, 0, pos.y), remove = false });
                }
            }

            // when we rotate the game object there is an offset needed to keep the object at the origin
            Vector2Int rotationOffset = _placedObjectSO.GetRotationOffset(dir);
            Vector3 placedObjectWorldPosition = mapGrid.GetWorldPosition(_x, _z) +
                new Vector3(rotationOffset.x, 0, rotationOffset.y) * mapGrid.GetCellSize();

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
                GridObject tempObject = mapGrid.GetGridObject(pos.x, pos.y);
                tempObject.isBuilding = true;
                tempObject.SetPlacedObject(genPO);
                tempObject.dCost += 20;
            }
        }
        else
        {
            //Debug.Log("can't build here");
        }
    }
}