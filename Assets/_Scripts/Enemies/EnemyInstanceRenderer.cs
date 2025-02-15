using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;

public class EnemyInstanceRenderer : MonoBehaviour
{
    [SerializeField] private Mesh enemyMesh;
    [SerializeField] private Material enemyMaterial;
    private Matrix4x4[] matrices;
    private const int BATCH_SIZE = 1023; // Maximum batch size for DrawMeshInstanced

    public void UpdateEnemyInstances(NativeList<EnemyData> enemyDataList)
    {
        // Initialize or resize matrices array if needed
        if (matrices == null || matrices.Length != enemyDataList.Length)
        {
            matrices = new Matrix4x4[enemyDataList.Length];
        }

        // Update transformation matrices
        for (int i = 0; i < enemyDataList.Length; i++)
        {
            matrices[i] = Matrix4x4.TRS(
                enemyDataList[i].Position,
                enemyDataList[i].Rotation,
                Vector3.one  // Scale - adjust if needed
            );
        }

        // Draw instances in batches
        int batchCount = (enemyDataList.Length + BATCH_SIZE - 1) / BATCH_SIZE;
        for (int i = 0; i < batchCount; i++)
        {
            int batchSize = Mathf.Min(BATCH_SIZE, enemyDataList.Length - i * BATCH_SIZE);
            var batchedMatrices = new Matrix4x4[batchSize];
            System.Array.Copy(matrices, i * BATCH_SIZE, batchedMatrices, 0, batchSize);
            
            Graphics.DrawMeshInstanced(enemyMesh, 0, enemyMaterial, batchedMatrices);
        }
    }
} 