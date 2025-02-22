using System.Collections.Generic;
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

    private List<GameObject> groundObjects = new List<GameObject>();
    public List<Vector3> blockedCoords = new List<Vector3>();
    
    

    private float randomOffsetX; // Random X offset for noise
    private float randomOffsetZ; // Random Z offset for noise

    public void InitTerrain()
    {
        // Generate random offsets for the Perlin noise
        randomOffsetX = Random.Range(0f, 1000f);
        randomOffsetZ = Random.Range(0f, 1000f);

        GenerateVertices();
        LowerVerticesAtCentre();
        LowerVerticesAtEdges();
        SplitTerrainIntoQuadrants(4);

        blockedCoords = ReturnBlockedCells();
        for (int i = 0; i < blockedCoords.Count; ++i)
        {
            //Instantiate(tempSphere, tempList[i], Quaternion.identity);
            MapGridManager.Instance.mapGrid.GetGridObject(blockedCoords[i]).isWalkable = false;
        }
        collisionManager.CreateTerrainArray(blockedCoords);
    }

    private void Update()
    {
        //if (Input.GetKeyDown(KeyCode.P)) { InitTerrain(); }
    }

    void GenerateVertices()
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

    private void LowerVerticesAtCentre()
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

                    vertexDouble[gridPosX,     gridPosJ    ].y = 0;
                    vertexDouble[gridPosX + 1, gridPosJ    ].y = 0;
                    vertexDouble[gridPosX,     gridPosJ + 1].y = 0;
                    vertexDouble[gridPosX + 1, gridPosJ + 1].y = 0;
                }
            }
        }
    }
    private void LowerVerticesAtEdges()
    {
        for (int x = 0; x < length+1; x++)
        {
            for (int z = 0; z < length+1; z++)
            {
                int innerLen = 15;
                int outerLen = length - innerLen;
                if (x < innerLen || x >= outerLen || z < innerLen || z >= outerLen)
                {
                    // Set the y position to 0 for vertices within this range
                    vertexDouble[x, z].y = 0f;
                }
            }
        }
    }

    void SplitTerrainIntoQuadrants(int iterations)
    {
        int gridSize = (int)Mathf.Pow(2, iterations);  // Number of divisions in each axis (e.g., 2x2, 4x4, etc.)
        int quadrantSize = length / gridSize;          // Size of each quadrant

        for (int i = 0; i < groundObjects.Count; i++)
        {
            Destroy(groundObjects[i]);
        }

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

    private List<Vector3> GroupAreas()
    {
        // Calculate number of vertices along x and z axes
        int xSize = Mathf.RoundToInt(length / vertexSpacing);
        int zSize = Mathf.RoundToInt(length / vertexSpacing);

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

    private List<Vector3> ReturnBlockedCells()
    {
        List<Vector3> regionVertices = new();

        for (int x = 0; x < length; x++)
        {
            for (int z = 0; z < length; z++)
            {
                if (vertexDouble[x, z].y > 0 || vertexDouble[x + 1, z].y > 0 ||
                    vertexDouble[x, z + 1].y > 0 || vertexDouble[x + 1, z + 1].y > 0)
                {
                    regionVertices.Add(new Vector3(x,0,z));
                }
            }
        }

        return regionVertices;
    }



    // Create a mesh for a specific quadrant
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
                Vector3 vertex = vertexDouble[x, z];
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
        groundObjects.Add(meshObject);
        MeshFilter meshFilter = meshObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = meshObject.AddComponent<MeshRenderer>();

        meshFilter.mesh = mesh;

        // Assign a material (for simplicity, use the default material)
        meshRenderer.material = groundMat;

        // Optionally, set the position of the object (if needed)
        meshObject.transform.position += new Vector3(0, -0.01f, 0);
    }

    private List<Vector3> GetNeighboursList(int _x, int _z)
    {
        List<Vector3> neighbourList = new List<Vector3>();

        if (_x - 1 >= 0)
        {
            //Left
            neighbourList.Add(vertexDouble[_x - 1, _z]);
            // Left Down
            if (_z - 1 >= 0) neighbourList.Add(vertexDouble[_x - 1, _z - 1]);
            // Left Up 
            if (_z + 1 < length) neighbourList.Add(vertexDouble[_x - 1, _z + 1]);
        }
        if (_x + 1 < length)
        {
            // Right
            neighbourList.Add(vertexDouble[_x + 1, _z]);
            // Left Down
            if (_z - 1 >= 0) neighbourList.Add(vertexDouble[_x + 1, _z - 1]);
            // Left Up 
            if (_z + 1 < length) neighbourList.Add(vertexDouble[_x + 1, _z + 1]);
        }
        // Down
        if (_z - 1 >= 0) neighbourList.Add(vertexDouble[_x, _z - 1]);
        // Up
        if (_z + 1 < length) neighbourList.Add(vertexDouble[_x, _z + 1]);

        return neighbourList;
    }
}
