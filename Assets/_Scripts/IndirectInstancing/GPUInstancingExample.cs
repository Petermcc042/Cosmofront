using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;
using System.Collections.Generic;

public class GPUInstancingExample : MonoBehaviour
{
    public Mesh mesh;
    public Material material;
    public int instanceCount = 100;

    private NativeArray<float3> positions;
    private NativeArray<Matrix4x4> matrices;
    private List<Matrix4x4> matrixList;

    private void Start()
    {
        // Initialize NativeArrays
        matrices = new NativeArray<Matrix4x4>(instanceCount, Allocator.Persistent);
        positions = new NativeArray<float3>(instanceCount, Allocator.Persistent);
        matrixList = new List<Matrix4x4>(instanceCount); // Preallocate

        // Assign initial positions
        for (int i = 0; i < instanceCount; i++)
        {
            positions[i] = new float3(
                UnityEngine.Random.Range(-100f, 100f),
                0f,
                UnityEngine.Random.Range(-100f, 100f));

            Quaternion rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
            Vector3 scale = Vector3.one * UnityEngine.Random.Range(0.5f, 1.5f);

            matrices[i] = Matrix4x4.TRS(positions[i], rotation, scale);
            matrixList.Add(matrices[i]); // Prepopulate list
        }

        material.enableInstancing = true;
    }

    private void Update()
    {
        // Schedule job
        MoveInstancesJob moveJob = new MoveInstancesJob
        {
            deltaTime = Time.deltaTime,
            positions = positions,
            matrices = matrices
        };

        JobHandle handle = moveJob.Schedule(instanceCount, 32);
        handle.Complete();

        // Update the matrix list without allocating a new array
        for (int i = 0; i < instanceCount; i++)
        {
            matrixList[i] = matrices[i];
        }

        // Render instances with the List (no allocation)
        Graphics.DrawMeshInstanced(mesh, 0, material, matrixList);
    }

    private void OnDestroy()
    {
        if (matrices.IsCreated) matrices.Dispose();
        if (positions.IsCreated) positions.Dispose();
    }

    [BurstCompile]
    private struct MoveInstancesJob : IJobParallelFor
    {
        public float deltaTime;
        public NativeArray<float3> positions;
        public NativeArray<Matrix4x4> matrices;

        public void Execute(int i)
        {
            float3 newPos = positions[i];
            newPos.y += math.sin(deltaTime * 2f + i) * 0.05f;
            positions[i] = newPos;

            matrices[i] = Matrix4x4.TRS(newPos, quaternion.identity, new float3(1, 1, 1));
        }
    }
}
