using UnityEngine;
using Unity.Collections;
using Unity.Mathematics; // For float4x4 if needed
using System.IO;

// Assuming EnemyData already has Position and Rotation (float3 or quaternion)
// You might need to add animation state here or in a separate list/array.
public struct EnemyGpuData // Or modify your existing EnemyData
{
    public float3 Position;
    public quaternion Rotation;
    public float CurrentAnimTime; // Or frame index + interpolation factor
    // Add other per-instance data if needed (e.g., color, health for shader effects)
}

public class EnemyGPURenderer : MonoBehaviour
{
    public Mesh enemyMesh; // Assign the base enemy mesh in the Inspector
    public Material enemyMaterial; // Assign a material using your custom GPU animation shader

    // Baked Animation Data
    [Header("Data Source")]
    public BakedDataReference dataRef; // Assign your BakedDataReference SO
    public Vector3[][] loadedFrames; // Your existing baked data [frame][vertex]
    private ComputeBuffer _animationVertexBuffer; // Buffer holding ALL vertex positions for ALL frames

    // Per-Instance Data
    private ComputeBuffer _enemyInstanceDataBuffer; // Buffer holding EnemyGpuData for each enemy
    private NativeList<EnemyGpuData> _enemyGpuDataList; // Temporary list to gather data before sending to GPU

    // Instancing Args
    private ComputeBuffer _argsBuffer;
    private uint[] _args = new uint[5] { 0, 0, 0, 0, 0 };

    private const int MAX_ENEMIES = 50000; // Example: Set a reasonable maximum

    void Awake()
    {
        LoadData();
        InitializeBuffers();
        // Load your loadedFrames data here...
        LoadAnimationDataToGPU();
    }

    void LoadData()
    {
        if (dataRef == null) { Debug.LogError("Data Reference (SO) is not assigned!"); return; }
        string fullPath = Path.Combine(Application.streamingAssetsPath, dataRef.binaryDataPath);
        if (!File.Exists(fullPath)) { Debug.LogError($"Binary data file not found at: {fullPath}"); return; }
        loadedFrames = new Vector3[dataRef.frameCount][];
        try
        {
            using (FileStream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                int fileVertexCount = reader.ReadInt32();
                int fileFrameCount = reader.ReadInt32();
                int fileFrameRate = reader.ReadInt32();
                if (fileVertexCount != dataRef.vertexCount || fileFrameCount != dataRef.frameCount || fileFrameRate != dataRef.frameRate)
                { Debug.LogError($"Metadata mismatch! Halting load."); loadedFrames = null; return; }

                for (int f = 0; f < dataRef.frameCount; f++)
                {
                    Vector3[] vertices = new Vector3[dataRef.vertexCount];
                    for (int v = 0; v < dataRef.vertexCount; v++)
                    {
                        vertices[v] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                    }
                    loadedFrames[f] = vertices;
                }
            }
        }
        catch (System.Exception e)
        { Debug.LogError($"Failed to read binary data: {e.Message}"); loadedFrames = null; }
    }

    void InitializeBuffers()
    {
        // Instance Data Buffer (Position, Rotation, AnimTime)
        // Stride calculation depends on the exact layout of EnemyGpuData
        // sizeof(float3) + sizeof(quaternion) + sizeof(float) = 12 + 16 + 4 = 32 bytes
        int instanceDataStride = System.Runtime.InteropServices.Marshal.SizeOf<EnemyGpuData>();
        _enemyInstanceDataBuffer = new ComputeBuffer(MAX_ENEMIES, instanceDataStride, ComputeBufferType.Default); // Or StructuredBuffer if preferred

        _enemyGpuDataList = new NativeList<EnemyGpuData>(Allocator.Persistent); // Use Allocator.TempJob if filled by a job each frame

        // Indirect Args Buffer for DrawMeshInstancedIndirect
        _argsBuffer = new ComputeBuffer(1, _args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        _args[0] = enemyMesh.GetIndexCount(0); // index count per instance
        _args[1] = 0;                         // instance count (will be updated)
        _args[2] = enemyMesh.GetIndexStart(0); // start index location
        _args[3] = enemyMesh.GetBaseVertex(0); // base vertex location
        _args[4] = 0;                         // start instance location
        _argsBuffer.SetData(_args);

        // --- Animation Vertex Data Buffer ---
        // This needs careful setup based on how you baked 'loadedFrames'
    }

    void LoadAnimationDataToGPU()
    {
        if (loadedFrames == null || loadedFrames.Length == 0 || loadedFrames[0].Length == 0)
        {
            Debug.LogError("Baked animation data is not loaded!");
            return;
        }

        int frameCount = loadedFrames.Length;
        int vertexCount = loadedFrames[0].Length;
        int totalVertices = frameCount * vertexCount;

        // Create a flat array of Vector3
        Vector3[] allVertexPositions = new Vector3[totalVertices];
        for (int f = 0; f < frameCount; f++)
        {
            for (int v = 0; v < vertexCount; v++)
            {
                allVertexPositions[f * vertexCount + v] = loadedFrames[f][v];
            }
        }

        // Create and fill the compute buffer
        // Stride is sizeof(Vector3) = 12 bytes
        _animationVertexBuffer = new ComputeBuffer(totalVertices, 12, ComputeBufferType.Default); // Or StructuredBuffer
        _animationVertexBuffer.SetData(allVertexPositions);

        // --- Pass animation info to the material ---
        enemyMaterial.SetBuffer("_AnimationVertices", _animationVertexBuffer);
        enemyMaterial.SetInt("_VertexCountPerFrame", vertexCount);
        enemyMaterial.SetInt("_FrameCount", frameCount);
        enemyMaterial.SetFloat("_AnimationLength", frameCount / 30.0f); // Assuming 30fps bake rate
        enemyMaterial.SetFloat("_FramesPerSecond", 30.0f); // Example FPS
    }


    // Call this from your main thread manager AFTER the jobs have updated enemyDataList
    public void UpdateEnemyDataForGPU(NativeList<EnemyData> enemyDataList)//, NativeList<float> enemyAnimationTimes) // Assuming you have anim times now
    {
        _enemyGpuDataList.Clear(); // Clear previous frame's data

        if (enemyDataList.Length > MAX_ENEMIES)
        {
            Debug.LogWarning($"Exceeding MAX_ENEMIES ({MAX_ENEMIES}). Clamping count.");
            // Optional: Resize buffer here if dynamic size is needed, but that's slower.
        }

        int count = Mathf.Min(enemyDataList.Length, MAX_ENEMIES);

        // This loop could potentially be a Job if EnemyData and EnemyGpuData are blittable
        for (int i = 0; i < count; i++)
        {
            _enemyGpuDataList.Add(new EnemyGpuData
            {
                Position = enemyDataList[i].Position,
                Rotation = enemyDataList[i].Rotation,
                CurrentAnimTime = 0//enemyAnimationTimes[i] // Get current animation time for this enemy
            });
        }

        // Upload the data to the GPU buffer
        _enemyInstanceDataBuffer.SetData(_enemyGpuDataList.AsArray(), 0, 0, count); // Only upload active count

        // Update the instance count in the args buffer for DrawMeshInstancedIndirect
        _args[1] = (uint)count;
        _argsBuffer.SetData(_args);

        // --- Set the per-instance buffer on the material ---
        enemyMaterial.SetBuffer("_PerInstanceData", _enemyInstanceDataBuffer);
    }


    void LateUpdate()
    {
        if (_enemyInstanceDataBuffer == null || _argsBuffer == null || enemyMesh == null || enemyMaterial == null || _enemyGpuDataList.Length == 0)
            return; // Don't draw if nothing to draw or not initialized

        // Define the bounds for culling (needs to encompass all potential enemy positions)
        Bounds bounds = new Bounds(Vector3.zero, Vector3.one * 10000f); // Adjust bounds as needed for your map size

        // Draw all instances!
        Graphics.DrawMeshInstancedIndirect(
            enemyMesh,
            0, // submesh index
            enemyMaterial,
            bounds,
            _argsBuffer,
            0, // args offset
            null, // MaterialPropertyBlock (optional, can override material properties per draw)
            UnityEngine.Rendering.ShadowCastingMode.On, // Cast shadows
            true // Receive shadows
        );
    }


    void OnDestroy()
    {
        // IMPORTANT: Release compute buffers when done
        _animationVertexBuffer?.Release();
        _animationVertexBuffer = null;

        _enemyInstanceDataBuffer?.Release();
        _enemyInstanceDataBuffer = null;

        _argsBuffer?.Release();
        _argsBuffer = null;

        if (_enemyGpuDataList.IsCreated)
        {
            _enemyGpuDataList.Dispose();
        }
    }
}