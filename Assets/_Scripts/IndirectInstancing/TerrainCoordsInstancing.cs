using UnityEngine;
using Unity.Mathematics;
using System.Collections.Generic;

public class TerrainCoordsInstancing : MonoBehaviour
{
    public Mesh mesh;
    public Material material;
    private int instanceCount = 100;

    private List<Matrix4x4> matrixList;
    private bool isCreated = false;

    public void ShowGridNodes()
    {
        instanceCount = PrecomputedData.gridArray.Length;

        matrixList = new List<Matrix4x4>(instanceCount); // Preallocate

        // Assign initial positions
        for (int i = 0; i < instanceCount; i++)
        {
            if (PrecomputedData.gridArray[i].isWalkable) { continue; }

            float3 position = new float3(PrecomputedData.gridArray[i].position);

            Quaternion rotation = Quaternion.identity;
            Vector3 scale = Vector3.one * 0.2f;

            matrixList.Add(Matrix4x4.TRS(position, rotation, scale)); // Prepopulate list
        }

        material.enableInstancing = true;
        isCreated = true;
    }

    private void Update()
    {
        if (!isCreated) { return; }
        Graphics.DrawMeshInstanced(mesh, 0, material, matrixList);
    }
}
