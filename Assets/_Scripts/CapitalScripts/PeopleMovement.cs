using UnityEngine;
using Unity.Mathematics;
using System.Collections.Generic;

public class PeopleMovement : MonoBehaviour
{
    public Mesh mesh;
    public Material material;
    public int instanceCount = 100;

    private List<Matrix4x4> matrixList;
    private bool isCreated = false;

    private void Awake()
    {
        matrixList = new List<Matrix4x4>();

        for (int i = 0; i < instanceCount; i++)
        {
            float3 position = new float3(
                UnityEngine.Random.Range(-100f, 100f),
                0f,
                UnityEngine.Random.Range(-100f, 100f));

            Quaternion rotation = Quaternion.identity;

            Vector3 scale = Vector3.one * UnityEngine.Random.Range(0.5f, 1.5f);

            matrixList.Add(Matrix4x4.TRS(position, rotation, scale));
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