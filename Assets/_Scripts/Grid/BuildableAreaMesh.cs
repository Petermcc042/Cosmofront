using UnityEngine;
using System.Collections.Generic;

public class BuildableAreaMesh : MonoBehaviour
{
    public List<Vector3> buildableAreas; // List of buildable area positions
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private MeshRenderer meshRenderer;

    public void InitMesh(bool _active)
    {
        GenerateMesh();
        gameObject.SetActive(_active);
    }

    public void GenerateMesh()
    {
        Mesh mesh = new Mesh();
        meshFilter.mesh = mesh;

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        int index = 0;
        foreach (var area in buildableAreas)
        {
            // Define the vertices for each square
            vertices.Add(new Vector3(area.x, 0, area.z)); // Bottom-left
            vertices.Add(new Vector3(area.x + 1, 0, area.z)); // Bottom-right
            vertices.Add(new Vector3(area.x, 0, area.z + 1)); // Top-left
            vertices.Add(new Vector3(area.x + 1, 0, area.z + 1)); // Top-right

            // Define the triangles for each square
            triangles.Add(index);
            triangles.Add(index + 2);
            triangles.Add(index + 1);
            triangles.Add(index + 2);
            triangles.Add(index + 3);
            triangles.Add(index + 1);

            // Define UVs for each square
            uvs.Add(new Vector2(0, 0));
            uvs.Add(new Vector2(1, 0));
            uvs.Add(new Vector2(0, 1));
            uvs.Add(new Vector2(1, 1));

            index += 4;
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();
    }
}