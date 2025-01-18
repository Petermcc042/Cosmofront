using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TerrainMeshGenerator : MonoBehaviour
{
    public float width = 100f;          // Width of the mesh
    public float depth = 100f;          // Depth of the mesh
    public float heightScale = 5f;      // Controls the amplitude of height variations
    public float noiseScale = 0.1f;     // Controls the frequency of the noise
    public float vertexSpacing = 0.1f;  // Distance between vertices (controls resolution)
    public float heightThreshold = 0.2f; // Threshold for flattening (between 0 and 1)

    private Mesh mesh;
    private Vector3[] vertices;
    private Vector3[,] vertexDouble;
    private int[] triangles;
    private Vector2[] uvs;

    private float randomOffsetX; // Random X offset for noise
    private float randomOffsetZ; // Random Z offset for noise

    private List<Vector3> validGridPositions = new List<Vector3>();
    [SerializeField] private GameObject tempCircle;


    void Start()
    {
        // Generate random offsets for the Perlin noise
        randomOffsetX = Random.Range(0f, 1000f);
        randomOffsetZ = Random.Range(0f, 1000f);

        GenerateMesh();
        List<Vector3> tempList = GroupAreas();
        Debug.Log(tempList.Count);


        for (int i = 0; i < tempList.Count; i++)
        {
            Instantiate(tempCircle, tempList[i], Quaternion.identity);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            // Generate random offsets for the Perlin noise
            randomOffsetX = Random.Range(0f, 1000f);
            randomOffsetZ = Random.Range(0f, 1000f);

            GenerateMesh();
        }
    }


    void GenerateMesh()
    {
        validGridPositions.Clear();

        // Calculate number of vertices along x and z axes
        int xSize = Mathf.RoundToInt(width / vertexSpacing);
        int zSize = Mathf.RoundToInt(depth / vertexSpacing);

        // Total number of vertices
        int vertCount = (xSize + 1) * (zSize + 1);

        // Initialize arrays
        vertices = new Vector3[vertCount];
        vertexDouble = new Vector3[xSize+1,zSize+1];
        uvs = new Vector2[vertCount];
        triangles = new int[xSize * zSize * 6];

        // Generate vertices
        int vertIndex = 0;
        for (int z = 0; z <= zSize; z++)
        {
            for (int x = 0; x <= xSize; x++)
            {

                float xPos = x * vertexSpacing;
                float zPos = z * vertexSpacing;

                // Generate height using Perlin noise with random offsets
                float yPos = Mathf.PerlinNoise((xPos * noiseScale) + randomOffsetX, (zPos * noiseScale) + randomOffsetZ) * heightScale;

                // Apply height threshold: if height is below threshold, set to zero
                if (yPos / heightScale < heightThreshold)
                {
                    yPos = 0;
                }

                // If the height is greater than zero, store the position
                if (yPos > 0)
                {
                    validGridPositions.Add(new Vector3(xPos, yPos, zPos));
                }

                vertexDouble[x, z] = new Vector3(xPos, yPos, zPos);

                vertices[vertIndex] = new Vector3(xPos, yPos, zPos);
                uvs[vertIndex] = new Vector2((float)x / xSize, (float)z / zSize);
                vertIndex++;
            }
        }

        // Generate triangles
        int triIndex = 0;
        for (int z = 0; z < zSize; z++)
        {
            for (int x = 0; x < xSize; x++)
            {
                int topLeft = z * (xSize + 1) + x;
                int bottomLeft = (z + 1) * (xSize + 1) + x;

                // First triangle
                triangles[triIndex++] = topLeft;
                triangles[triIndex++] = bottomLeft;
                triangles[triIndex++] = topLeft + 1;

                // Second triangle
                triangles[triIndex++] = topLeft + 1;
                triangles[triIndex++] = bottomLeft;
                triangles[triIndex++] = bottomLeft + 1;
            }
        }

        // Create mesh
        mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();

        // Assign mesh to MeshFilter
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh = mesh;

        // Position the mesh correctly
        transform.position = new Vector3(0, -0.02f, 0);
        transform.localScale = new Vector3(1, 1, 1);
    }

    private List<Vector3> GroupAreas()
    {
        // Calculate number of vertices along x and z axes
        int xSize = Mathf.RoundToInt(width / vertexSpacing);
        int zSize = Mathf.RoundToInt(depth / vertexSpacing);

        List<Vector3> regionVertices = new();

        for (int z = 0; z <= zSize; z++)
        {
            for (int x = 0; x <= xSize; x++)
            {
                if (vertexDouble[x, z].y > 0)
                {
                    List<Vector3> tempList = GetNeighboursList(x, z);
                    foreach (var coord in tempList)
                    {
                        if (coord.y <= 0)
                        {
                            regionVertices.Add(vertexDouble[x, z]);
                            break;
                        }
                    }
                }

            }
        }

        return regionVertices;
    }

    private List<Vector3> GetNeighboursList(int _x, int _z)
    {
        List<Vector3> neighbourList = new List<Vector3>();

        if (_x - 1 >= 0)
        {
            //Left
            neighbourList.Add(vertexDouble[_x - 1, _z]);
            // Left Down
            if (_z - 1 >= 0) neighbourList.Add(vertexDouble[_x - 1, _z -1]);
            // Left Up 
            if (_z + 1 < width) neighbourList.Add(vertexDouble[_x - 1, _z + 1]);
        }
        if (_x + 1 < width)
        {
            // Right
            neighbourList.Add(vertexDouble[_x + 1, _z ]);
            // Left Down
            if (_z - 1 >= 0) neighbourList.Add(vertexDouble[_x + 1, _z - 1]);
            // Left Up 
            if (_z + 1 < width) neighbourList.Add(vertexDouble[_x + 1, _z + 1]);
        }
        // Down
        if (_z - 1 >= 0) neighbourList.Add(vertexDouble[_x, _z - 1]);
        // Up
        if (_z + 1 < width) neighbourList.Add(vertexDouble[_x, _z + 1]);

        return neighbourList;
    }
}


