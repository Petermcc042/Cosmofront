using UnityEngine;
using Unity.Mathematics;
using System.Collections.Generic;

public class GridCoordInstancing : MonoBehaviour
{
    public Mesh mesh;
    public Material pathfindingMaterial;
    public Material baseMaterial;
    private int instanceCount = 100;

    private List<Matrix4x4> pathfindingMatrixList;
    private List<Matrix4x4> baseMatrixList;
    private bool isCreated = false;

    public bool showNodes = false;

    public void ShowGridNodes()
    {
        instanceCount = PrecomputedData.gridArray.Length;

        pathfindingMatrixList = new List<Matrix4x4>();
        baseMatrixList = new List<Matrix4x4>();

        for (int i = 0; i < instanceCount; i++)
        {
            GridNode tempNode = PrecomputedData.gridArray[i];

/*            if (tempNode.isPathfindingArea && !tempNode.isBaseArea) 
            {
                float3 position = new float3(tempNode.position);

                Quaternion rotation = Quaternion.identity;

                Vector3 scale = Vector3.one * 0.2f;

                pathfindingMatrixList.Add(Matrix4x4.TRS(position, rotation, scale)); 
            }*/

            if (tempNode.isBaseArea)
            {
                float3 position = new float3(tempNode.position);

                Quaternion rotation = Quaternion.identity;

                Vector3 scale = Vector3.one * 0.2f;

                baseMatrixList.Add(Matrix4x4.TRS(position, rotation, scale));
            }
        }

        pathfindingMaterial.enableInstancing = true;
        baseMaterial.enableInstancing = true;
        isCreated = true;
    }

    private void Update()
    {
        if (!isCreated || !showNodes) { return; }
        //Graphics.DrawMeshInstanced(mesh, 0, pathfindingMaterial, pathfindingMatrixList);
        Graphics.DrawMeshInstanced(mesh, 0, baseMaterial, baseMatrixList);
    }
}
