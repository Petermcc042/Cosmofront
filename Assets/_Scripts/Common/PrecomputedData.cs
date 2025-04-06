using System;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
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

    public static bool creationNeeded;
    public static int length;
    public static float cellSize;
    private static Vector3 originPosition;
    public static NativeArray<GridNode> gridArray;
    private static PlacedObject[] placedObjectArray;


    // Terrain Generation
    private static float randomOffsetX; // Random X offset for noise
    private static float randomOffsetZ; // Random Z offset for noise
    private static float heightScale;      // Controls the amplitude of height variations
    private static float noiseScale = 0.04f;     // Controls the frequency of the noise
    private static float vertexSpacing = 1f;  // Distance between vertices (controls resolution)
    private static float heightThreshold = 0.6f; // Threshold for flattening (between 0 and 1)
    public static Vector3[,] vertexDouble;
    public static List<float3> terrainCoords;

    //[RuntimeInitializeOnLoadMethod]
    public static void Init(int _length) 
    {
        creationNeeded = true;
        length = _length;
        cellSize = 1;
        originPosition = Vector3.zero;
        heightScale = 2f;
        noiseScale = 0.04f;
        vertexSpacing = 1f;
        heightThreshold = 0.6f;

        randomOffsetX = 0;
        randomOffsetZ = 0;

        if (gridArray.IsCreated) { gridArray.Dispose(); }
        gridArray = new NativeArray<GridNode>(length * length, Allocator.Persistent);
        terrainCoords = new List<float3>();
        placedObjectArray = new PlacedObject[length * length];
        vertexDouble = new Vector3[length + 1, length + 1]; // will break if the vertex spacing changes
    }


    public static void InitGrid()
    {
        CreateTerrain();
        CreateGrid();
        SaveVerticesToCSV(vertexDouble, "verticesDebug.csv");
        WriteDataToCSVPrior("precomputed_grid_prior.csv");
        RunFlowFieldJobs(99,99,true);

        WriteDataToCSV("precomputed_grid.csv");
        creationNeeded = false;
    }

    private static void CreateTerrain()
    {
        randomOffsetX = UnityEngine.Random.Range(0f, 1000f);
        randomOffsetZ = UnityEngine.Random.Range(0f, 1000f);
        GenerateVertices();
        LowerVerticesAtCentre();
        LowerVerticesAtEdges();
    }

    public static void CreateGrid() {
        int count = 0;
        for (int x = 0; x < length; x++)
        {
            for (int z = 0; z < length; z++)
            {
                bool tempWalk = true;

                if (vertexDouble[x, z].y > 0 || vertexDouble[x + 1, z].y > 0 || vertexDouble[x, z + 1].y > 0 || vertexDouble[x + 1, z + 1].y > 0)
                {
                    tempWalk = false;
                }

                GridNode tempNode = new() {
                    position = new float3(x, 0, z),
                    index = count,
                    x = x, z = z,
                    cost = 0,
                    integrationCost = 0,
                    goToIndex = 0,
                    isWalkable = tempWalk,
                    isPathfindingArea = false,
                    isBuilding = false,
                    isTraversable = false,
                    isBaseArea = false
                };

                gridArray[count] = tempNode;
                count++;
            }
        }
    }

    public static void SaveVerticesToCSV(Vector3[,] vertices, string filePath)
    {
        int rows = vertices.GetLength(0);
        int cols = vertices.GetLength(1);

        using (StreamWriter writer = new StreamWriter(filePath))
        {
            // Write header
            writer.WriteLine("x,y,z");

            // Write each vertex position
            for (int x = 0; x < rows; x++)
            {
                for (int z = 0; z < cols; z++)
                {
                    Vector3 vertex = vertices[x, z];
                    string line = $"{vertex.x},{vertex.y},{vertex.z}";
                    writer.WriteLine(line);
                }
            }
        }

        Debug.Log($"Vertices saved to {filePath}");
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


    private static void GenerateVertices()
    {
        // Calculate the number of vertices along x and z axes based on vertex spacing
        int vertCount = Mathf.RoundToInt(length / vertexSpacing);

        vertexDouble = new Vector3[vertCount + 1, vertCount + 1];

        for (int x = 0; x <= vertCount; x++)
        {
            for (int z = 0; z <= vertCount; z++)
            {
                // Ensure vertices cover the entire length (200 units), regardless of vertex spacing
                float xPos = ((float)x / vertCount) * length;
                float zPos = ((float)z / vertCount) * length;

                // Generate height using Perlin noise with random offsets
                float yPos = Mathf.PerlinNoise((xPos * noiseScale) + randomOffsetX, (zPos * noiseScale) + randomOffsetZ) * heightScale;

                if (yPos / heightScale < heightThreshold)
                {
                    yPos = 0;
                }

                vertexDouble[x, z] = new Vector3(xPos, yPos, zPos);
            }
        }
    }

    private static void LowerVerticesAtCentre()
    {
        int radius = 8;
        int center = length / 2;

        for (int i = -radius; i <= radius; i++)
        {
            for (int j = -radius; j <= radius; j++)
            {
                if (i * i + j * j <= radius * radius)
                {
                    int gridPosX = center + i;
                    int gridPosJ = center + j;

                    vertexDouble[gridPosX, gridPosJ].y = 0;
                    vertexDouble[gridPosX + 1, gridPosJ].y = 0;
                    vertexDouble[gridPosX, gridPosJ + 1].y = 0;
                    vertexDouble[gridPosX + 1, gridPosJ + 1].y = 0;
                }
            }
        }
    }
    private static void LowerVerticesAtEdges()
    {
        for (int x = 0; x < length + 1; x++)
        {
            for (int z = 0; z < length + 1; z++)
            {
                int innerLen = 30;
                int outerLen = length - innerLen;
                if (x < innerLen || x >= outerLen || z < innerLen || z >= outerLen)
                {
                    // Set the y position to 0 for vertices within this range
                    vertexDouble[x, z].y = 0f;
                }
            }
        }
    }


    public static void RunFlowFieldJobs(int _endX, int _endZ, bool _runFullFlow) {
        
        var updateCostJob = new UpdateNodesMovementCost
        {
            GridArray = gridArray,
            endX = _endX,
            endZ = _endZ,
            runFullGrid = _runFullFlow
        };
        JobHandle handle1 = updateCostJob.Schedule(gridArray.Length, 64);
        handle1.Complete();


        NativeQueue<GridNode> nodeQueue = new NativeQueue<GridNode>(Allocator.Persistent);

        UpdateNodesIntegration updateIntegrationJob = new UpdateNodesIntegration
        {
            GridArray = gridArray,
            NodeQueue = nodeQueue,
            endX = _endX,
            endZ = _endZ,
            gridWidth = length,
            runFullGrid = _runFullFlow
        };
        //JobHandle handle2 = updateIntegrationJob.Schedule();
        //handle2.Complete();
        updateIntegrationJob.Execute();

        nodeQueue.Dispose();

        WeightBuildingNodes flowJob3 = new WeightBuildingNodes
        {
            GridArray = gridArray,
            endX = _endX,
            endZ = _endZ,
            gridWidth = length
        };

        JobHandle handle3 = flowJob3.Schedule();
        handle3.Complete();


        UpdateGoToIndex flowJob4 = new UpdateGoToIndex
        {
            GridArray = gridArray,
            endX = _endX,
            endZ = _endZ,
            runFullGrid = _runFullFlow,
            gridWidth = length
        };

        JobHandle flowHandle4 = flowJob4.Schedule();
        flowHandle4.Complete();
    }

    public static void WriteDataToCSV(string filePath)
    {
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            // Write the header
            writer.WriteLine("index,goToIndex,integrationCost,position,goToPosition");

            // Write each flowNode's data
            for (int i = 0; i < gridArray.Length; i++)
            {
                if (gridArray[i].goToIndex < 0)
                {
                    string line = $"{gridArray[i].index},{gridArray[i].goToIndex},{gridArray[i].integrationCost},{gridArray[i].x}:{gridArray[i].z},0";
                    writer.WriteLine(line);
                }
                else
                {
                    string line = $"{gridArray[i].index},{gridArray[i].goToIndex},{gridArray[i].integrationCost},{gridArray[i].x}:{gridArray[i].z},{gridArray[gridArray[i].goToIndex].x}:{gridArray[gridArray[i].goToIndex].z}";
                    writer.WriteLine(line);
                }

            }
        }
    }

    public static void WriteDataToCSVPrior(string filePath)
    {
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            // Write the header
            writer.WriteLine("position.x,position.y,position.z,walkable");

            // Write each flowNode's data
            for (int i = 0; i < gridArray.Length; i++)
            {
                string line = $"{gridArray[i].position.x},{gridArray[i].position.y},{gridArray[i].position.z},{gridArray[i].isWalkable}";
                writer.WriteLine(line);
            }
        }
    }
}