using UnityEngine;

public class GhostMeshGenerator : MonoBehaviour
{
    void Start()
    {
        // Create the Mesh
        Mesh ghostMesh = new Mesh();

        // Define vertices (a simple ghost shape)
        Vector3[] vertices = new Vector3[]
        {
            new Vector3(-0.5f, 0f, 0f),  // Bottom-left
            new Vector3(0.5f, 0f, 0f),   // Bottom-right
            new Vector3(-0.5f, 1f, 0f),  // Top-left
            new Vector3(0.5f, 1f, 0f),   // Top-right
            new Vector3(0f, 1.5f, 0f)    // Top peak (ghost head)
        };

        // Define triangles (indices for the vertices)
        int[] triangles = new int[]
        {
            0, 2, 1,  // Bottom-left triangle
            1, 2, 3,  // Bottom-right triangle
            2, 4, 3   // Top triangle (ghost head)
        };

        // Define UVs (texture mapping coordinates)
        Vector2[] uvs = new Vector2[]
        {
            new Vector2(0f, 0f),  // Bottom-left
            new Vector2(1f, 0f),  // Bottom-right
            new Vector2(0f, 1f),  // Top-left
            new Vector2(1f, 1f),  // Top-right
            new Vector2(0.5f, 1.5f) // Top peak (ghost head)
        };

        // Assign data to the mesh
        ghostMesh.vertices = vertices;
        ghostMesh.triangles = triangles;
        ghostMesh.uv = uvs;

        // Recalculate normals for lighting
        ghostMesh.RecalculateNormals();

        // Create a GameObject to render the mesh
        GameObject ghost = new GameObject("Ghost", typeof(MeshFilter), typeof(MeshRenderer));
        ghost.GetComponent<MeshFilter>().mesh = ghostMesh;

        // Apply a semi-transparent material
        Material ghostMaterial = new Material(Shader.Find("Standard"));
        ghostMaterial.color = new Color(1f, 1f, 1f, 0.5f);  // White with 50% transparency
        ghostMaterial.SetFloat("_Mode", 3); // Transparent mode
        ghostMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        ghostMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        ghostMaterial.SetInt("_ZWrite", 0);
        ghostMaterial.DisableKeyword("_ALPHATEST_ON");
        ghostMaterial.EnableKeyword("_ALPHABLEND_ON");
        ghostMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        ghostMaterial.renderQueue = 3000;

        ghost.GetComponent<MeshRenderer>().material = ghostMaterial;
    }
}
