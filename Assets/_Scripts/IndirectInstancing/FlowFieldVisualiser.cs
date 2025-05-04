using System.Collections.Generic;
using UnityEngine;

public class FlowFieldVisualiser : MonoBehaviour
{
    private int instanceCount = 100; // Consider setting dynamically if needed elsewhere
    private bool isCreated = false;
    public bool showNodes = false;

    // --- New Variables for Directions ---
    [Header("Direction Visualization")]
    public Mesh directionMesh; // Assign an arrow or cone mesh here in the Inspector
    public Material directionMaterial;
    public float directionArrowScale = 0.18f; // Adjust scale as needed
    private List<Matrix4x4> directionMatrixList;

    public void ShowGridNodes()
    {
        if (PrecomputedData.gridArray == null || PrecomputedData.gridArray.Length == 0)
        {
            Debug.LogWarning("PrecomputedData.gridArray is not initialized or empty.");
            isCreated = false;
            return;
        }

        instanceCount = PrecomputedData.gridArray.Length;
        directionMatrixList = new List<Matrix4x4>(instanceCount); // Initialize direction list

        Vector3 arrowScale = Vector3.one * directionArrowScale;

        for (int i = 0; i < instanceCount; i++)
        {
            GridNode tempNode = PrecomputedData.gridArray[i];
            Vector3 position = (Vector3)tempNode.position; // Cast float3 to Vector3 for Unity functions

            // --- Pathfinding Node Sphere ---
            if (tempNode.isWalkable)
            {
                // --- Calculate and Add Direction Arrow Matrix ---
                if (directionMesh != null && directionMaterial != null && tempNode.goToIndex >= 0 && tempNode.goToIndex < PrecomputedData.gridArray.Length) // Ensure goToIndex is valid
                {
                    Vector3 goToPosition = (Vector3)PrecomputedData.gridArray[tempNode.goToIndex].position;
                    Vector3 direction = goToPosition - position;
                    Quaternion rotation = Quaternion.identity; // Default rotation

                    // Ensure direction is not zero vector before calculating LookRotation
                    if (direction.sqrMagnitude > 0.001f) // Use sqrMagnitude for efficiency
                    {
                        rotation = Quaternion.LookRotation(direction.normalized);
                        rotation *= Quaternion.Euler(90, 0, 0);
                        // --- IMPORTANT ---
                        // Quaternion.LookRotation makes the Z-axis point in the 'direction'.
                        // If your arrow mesh points forward along its local Y or X axis,
                        // you'll need an additional fixed rotation.
                        // Example if arrow points along Y: rotation *= Quaternion.Euler(90, 0, 0);
                        // Example if arrow points along X: rotation *= Quaternion.Euler(0, -90, 0);
                        // Adjust this based on how your arrow mesh is modeled.
                    }

                    directionMatrixList.Add(Matrix4x4.TRS(position, rotation, arrowScale));
                }
            }
        }

        if (directionMaterial != null) directionMaterial.enableInstancing = true; // Enable for directions

        isCreated = true;
    }

    private void Update()
    {
        if (!isCreated || !showNodes) { return; }

        if (directionMesh != null && directionMaterial != null && directionMatrixList.Count > 0)
        {
            Graphics.DrawMeshInstanced(directionMesh, 0, directionMaterial, directionMatrixList);
        }
    }
}