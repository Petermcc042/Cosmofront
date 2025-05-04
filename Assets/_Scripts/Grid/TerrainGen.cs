using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class TerrainGen : MonoBehaviour
{
    [SerializeField] private BuildableAreaMesh buildableAreaMesh;
    [SerializeField] private CollisionManager collisionManager;

    public int length = 200;          // Width of the mesh
    public float heightScale = 2f;      // Controls the amplitude of height variations
    public float noiseScale = 0.04f;     // Controls the frequency of the noise
    public float vertexSpacing = 1f;  // Distance between vertices (controls resolution)
    public float heightThreshold = 0.6f; // Threshold for flattening (between 0 and 1)
    public Vector3[,] vertexDouble;
    public Material groundMat;
    public GameObject tempSphere;

    public List<float3> blockedCoords = new List<float3>();
    public NativeArray<float3> blockedCoordsArray;


    [Header("For Visualisation")]
    public bool showCoords;
    public Mesh mesh;
    public Material material;
    private int instanceCount = 100;

    private List<Matrix4x4> matrixList;
    private bool isCreated = false;


    public void InitTerrain()
    {
/*        // Generate random offsets for the Perlin noise
        randomOffsetX = UnityEngine.Random.Range(0f, 1000f);
        randomOffsetZ = UnityEngine.Random.Range(0f, 1000f);

        GenerateVertices();
        LowerVerticesAtCentre();
        LowerVerticesAtEdges();*/

        //SplitTerrainIntoQuadrants(4);

        blockedCoordsArray = new NativeArray<float3>(PrecomputedData.terrainCoords.Count, Allocator.Persistent);


        for (int i = 0; i < PrecomputedData.terrainCoords.Count; ++i)
        {
            blockedCoordsArray[i] = PrecomputedData.terrainCoords[i];
        }

        collisionManager.CreateTerrainArray();

        if (showCoords) { ShowTerrainNodes(); }
    }

    public void ShowTerrainNodes()
    {
        instanceCount = blockedCoordsArray.Length;

        matrixList = new List<Matrix4x4>(instanceCount); // Preallocate

        // Assign initial positions
        for (int i = 0; i < instanceCount; i++)
        {
            Quaternion rotation = Quaternion.identity;
            Vector3 scale = new Vector3(1f, 1f * UnityEngine.Random.Range(1,5), 1f);

            matrixList.Add(Matrix4x4.TRS(blockedCoordsArray[i], rotation, scale)); // Prepopulate list
        }

        material.enableInstancing = true;
        isCreated = true;
    }

    private void Update()
    {
        if (!isCreated) { return; }
        Graphics.DrawMeshInstanced(mesh, 0, material, matrixList);
    }

    void SplitTerrainIntoQuadrants(int iterations)
    {
        int gridSize = (int)Mathf.Pow(2, iterations);  // Number of divisions in each axis (e.g., 2x2, 4x4, etc.)
        int quadrantSize = length / gridSize;          // Size of each quadrant

        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                // Calculate the bounds for the current quadrant
                int xStart = i * quadrantSize;
                int xEnd = (i + 1) * quadrantSize;
                int yStart = j * quadrantSize;
                int yEnd = (j + 1) * quadrantSize;

                // Create the mesh for the current quadrant
                Mesh quadrantMesh = CreateMeshForQuadrant(xStart, xEnd, yStart, yEnd);
                
                // Create a mesh object for the current quadrant
                string quadrantName = $"Quadrant {i * gridSize + j + 1}";
                CreateMeshObject(quadrantMesh, quadrantName);
            }
        }
    }




    // Create a mesh for a specific quadrant
    Mesh CreateMeshForQuadrant(int xStart, int xEnd, int zStart, int zEnd)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        List<Color> colors = new List<Color>();  // New list for vertex colors
        int vertIndex = 0;

        for (int z = zStart; z <= zEnd; z++)
        {
            for (int x = xStart; x <= xEnd; x++)
            {
                Vector3 vertex = PrecomputedData.vertexDouble[x, z];
                vertices.Add(vertex);

                // Calculate UVs based on position in the quadrant
                uvs.Add(new Vector2((float)(x - xStart) / (xEnd - xStart), (float)(z - zStart) / (zEnd - zStart)));

                // Assign color based on height (y value)
                float height = vertex.y;  // Height is the y value
                Color color = GetColorForHeight(height);  // Calculate color for this height
                colors.Add(color);  // Add the color to the list

                // Create triangles for each square in the mesh grid
                if (x < xEnd && z < zEnd)
                {
                    int topLeft = vertIndex;
                    int topRight = vertIndex + 1;
                    int bottomLeft = vertIndex + (xEnd - xStart + 1);
                    int bottomRight = vertIndex + (xEnd - xStart + 1) + 1;

                    triangles.Add(topLeft);
                    triangles.Add(bottomLeft);
                    triangles.Add(bottomRight);

                    triangles.Add(topLeft);
                    triangles.Add(bottomRight);
                    triangles.Add(topRight);
                }

                vertIndex++;
            }
        }

        // Create the mesh
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.colors = colors.ToArray();  // Assign vertex colors
        mesh.RecalculateNormals();

        return mesh;
    }

    Color GetColorForHeight(float height)
    {
        if (height < 0.2f)
            return Color.blue;  // Water or low altitude
        else if (height < 0.4f)
            return Color.green;  // Grassland
        else if (height < 0.7f)
            return new Color(0.5f, 0.25f, 0);  // Brown for mountains
        else
            return Color.white;  // Snowy peaks
    }



    // Create a new GameObject for a mesh
    void CreateMeshObject(Mesh mesh, string name)
    {
        GameObject meshObject = new GameObject(name);
        MeshFilter meshFilter = meshObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = meshObject.AddComponent<MeshRenderer>();

        meshFilter.mesh = mesh;

        // Assign a material (for simplicity, use the default material)
        meshRenderer.material = groundMat;

        // Optionally, set the position of the object (if needed)
        meshObject.transform.position += new Vector3(0, -0.01f, 0);
    }
}
