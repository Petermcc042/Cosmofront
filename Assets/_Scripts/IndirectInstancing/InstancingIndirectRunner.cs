using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine.Rendering; // Required for ComputeBufferType
using System.IO;

[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)] // Ensure layout matches HLSL
public struct InstanceDataGpu
{
    public float4x4 Matrix;         // 64 bytes
    public float AnimationFrame;    // 4 bytes (use float for flexibility, can be treated as int in shader)
    // Padding needed to reach a multiple of 16 bytes (float4 alignment)
    // Total size = 64 + 4 = 68. Next multiple of 16 is 80. Need 12 bytes padding.
    public float Padding1;          // 4 bytes
    public float Padding2;          // 4 bytes
    public float Padding3;          // 4 bytes
    // Total Stride = 80 bytes
}

public class InstancingIndirectRunner : MonoBehaviour
{
    [SerializeField] private EnemyManager enemyManager;

    [Header("Animation Data")]
    public BakedDataReference dataRef;
    public float playbackFramesPerSecond = 30.0f; // <<< Added back: Desired playback speed
    private quaternion _fixedXRotation;


    // --- Animation Compute Shader Variables ---
    private Vector3[][] loadedFrames;
    private int _vertexCountPerFrame = 0;
    private int _frameCountActual = 0;

    [Header("Instancing Settings")]
    public Mesh mesh;
    public Material material; // Assign a Material using the custom shader below
    public int instanceCount = 100;

    // --- CPU Data (for Job) ---
    public NativeList<EnemyData> enemyDataList;
    private NativeArray<InstanceDataGpu> instanceDataArray;
    private NativeArray<int> activeEnemyCountResult;

    // --- GPU Buffers ---
    private ComputeBuffer _InstanceDataBuffer;      // Holds InstanceDataGpu per instance
    private ComputeBuffer _AnimationVertexBuffer;   // Holds all vertex positions for all frames
    private ComputeBuffer _ArgsBuffer;

    // Indirect draw arguments: [index count per instance, instance count, start index location, base vertex location, start instance location]
    // We initialize element 1 (instance count) once in Start for this basic example.
    private uint[] _Args = new uint[5] { 0, 0, 0, 0, 0 };
    private Bounds _bounds; // Required bounds for drawing

    // Shader Property IDs
    private static readonly int InstanceDataBufferPropID = Shader.PropertyToID("_MatricesBuffer"); // HLSL buffer name (matches previous setup)
    private static readonly int AnimVertexBufferPropID = Shader.PropertyToID("_AnimationVertexBuffer");
    private static readonly int NumVertsPerFramePropID = Shader.PropertyToID("_NumVerticesPerFrame");
    private static readonly int NumAnimFramesPropID = Shader.PropertyToID("_NumAnimFrames");

    void Start()
    {
        if (!ValidateSetup()) return; // quick check for inspector assignments
        enemyDataList = enemyManager.enemyDataList;


        LoadAndPrepareFrameData();
        InitializeCPUData();
        InitializeGpuBuffers(); // Combined buffer init
        SetupDrawArguments();

        // --- Set Buffers & Uniforms for Material ---
        material.SetBuffer(InstanceDataBufferPropID, _InstanceDataBuffer);
        material.SetBuffer(AnimVertexBufferPropID, _AnimationVertexBuffer);
        material.SetInt(NumVertsPerFramePropID, mesh.vertexCount);
        material.SetInt(NumAnimFramesPropID, loadedFrames.Length);

        // Define the drawing bounds (needs to encompass all instances)
        // For simplicity, use a large fixed bounds. In a real game, calculate this more tightly.
        _bounds = new Bounds(new Vector3(99,0,99), Vector3.one * 100f);
        _fixedXRotation = quaternion.Euler(math.radians(-180.0f), 0f, 0f);
    }

    void InitializeCPUData()
    {
        instanceDataArray = new NativeArray<InstanceDataGpu>(instanceCount, Allocator.Persistent);
        Unity.Mathematics.Random random = new Unity.Mathematics.Random((uint)System.DateTime.Now.Ticks); // Use Unity.Mathematics.Random


        for (int i = 0; i < instanceCount; i++)
        {
            float3 position = random.NextFloat3(-5f, 5f);

            // --- Define the random rotation around Y-axis ---
            float yAngleRad = random.NextFloat(0f, 2f * math.PI);
            quaternion randomYRotation = quaternion.Euler(0f, yAngleRad, 0f);
            quaternion finalRotation = math.mul(randomYRotation, _fixedXRotation);

            float3 scale = new float3(1, 1, 1);
            float startFrame = 0f; // Start at frame 0

            instanceDataArray[i] = new InstanceDataGpu
            {
                Matrix = float4x4.TRS(position, finalRotation, scale), // Use the combined finalRotation
                AnimationFrame = startFrame
            };
        }
    }

    void InitializeGpuBuffers()
    {
        // Instance Buffer
        int instanceDataStride = System.Runtime.InteropServices.Marshal.SizeOf<InstanceDataGpu>(); // should equal 80
        _InstanceDataBuffer?.Release(); // Release previous if any
        _InstanceDataBuffer = new ComputeBuffer(instanceCount, instanceDataStride, ComputeBufferType.Default);
        _InstanceDataBuffer.SetData(instanceDataArray);

        // --- Animation Vertex Buffer (Flattened Vertex Data) ---
        Vector3[] flatVertexData = FlattenFrames(loadedFrames);
        _AnimationVertexBuffer?.Release();
        _AnimationVertexBuffer = new ComputeBuffer(flatVertexData.Length, sizeof(float) * 3); // Stride = 12 bytes for float3/Vector3
        _AnimationVertexBuffer.SetData(flatVertexData);


        // --- Indirect Arguments Buffer ---
        _ArgsBuffer = new ComputeBuffer(1, sizeof(uint) * 5, ComputeBufferType.IndirectArguments);
        // Args set in SetupDrawArguments
    }

    void SetupDrawArguments()
    {
        _Args[0] = (uint)mesh.GetIndexCount(0);    // 0: Index count per instance (using submesh 0)
        _Args[1] = (uint)instanceCount;            // 1: Instance count <--- KEY PART FOR INDIRECT
        _Args[2] = (uint)mesh.GetIndexStart(0);   // 2: Start index location
        _Args[3] = (uint)mesh.GetBaseVertex(0);    // 3: Base vertex location
        _Args[4] = 0;                              // 4: Start instance location (always 0 for this basic setup)

        // Upload the arguments to the GPU buffer
        _ArgsBuffer.SetData(_Args);
    }


    void Update()
    {
        if (_ArgsBuffer == null || _InstanceDataBuffer == null) return; // Don't run if buffers aren't ready

        MoveInstancesJob moveJob = new MoveInstancesJob
        {
            deltaTime = Time.deltaTime,
            playbackFramesPerSecond = this.playbackFramesPerSecond, // Use the public variable
            numFrames = this._frameCountActual,
            enemyDataList = this.enemyDataList,
            instanceData = instanceDataArray
        };

        JobHandle handle = moveJob.Schedule(instanceCount, 32);
        handle.Complete();


        // --- 2. Upload Updated Data to GPU Buffer ---
        _InstanceDataBuffer.SetData(instanceDataArray); // Copy data from NativeArray to ComputeBuffer

        // --- 3. Draw Instanced Indirect ---
        Graphics.DrawMeshInstancedIndirect(
            mesh,
            0,                  // Submesh index
            material,           // Material using the custom shader
            _bounds,            // Bounds for culling
            _ArgsBuffer,        // Buffer containing draw arguments (like instance count)
            0,                  // Offset in the args buffer (usually 0)
            null,               // Optional MaterialPropertyBlock
            ShadowCastingMode.On,// Enable shadow casting
            true                // Enable receiving shadows
                                // Camera parameter is implicitly Camera.main or scene view camera
        );
    }

    void OnDestroy()
    {
        // Dispose NativeArrays
        if (instanceDataArray.IsCreated) instanceDataArray.Dispose();

        // Release Compute Buffers
        _InstanceDataBuffer?.Release(); _InstanceDataBuffer = null;
        _AnimationVertexBuffer?.Release(); _AnimationVertexBuffer = null;
        _ArgsBuffer?.Release(); _ArgsBuffer = null;
    }

    [BurstCompile]
    private struct MoveInstancesJob : IJobParallelFor
    {
        [ReadOnly] public float deltaTime;
        [ReadOnly] public float playbackFramesPerSecond;
        [ReadOnly] public int numFrames; // Total frame count for looping
        [ReadOnly] public NativeList<EnemyData> enemyDataList;

        public NativeArray<InstanceDataGpu> instanceData;

        public void Execute(int i)
        {
            // Get the current matrix from the instance data
            float4x4 matrix = instanceData[i].Matrix;

            // Extract current position (which is in the 4th column)
            float3 currentPos = new float3(
                matrix.c3.x,
                matrix.c3.y,
                matrix.c3.z
            );

            // Update the Y position
            currentPos.y += math.sin(deltaTime * 2f + i) * 0.0005f;

            // Create a new instance data that preserves everything but updates the position
            InstanceDataGpu newData = instanceData[i];

            // Update only the position elements in the matrix (column 3)
            if (i < enemyDataList.Length)
            {
                newData.Matrix.c3.x = enemyDataList[i].Position.x;
                newData.Matrix.c3.y = enemyDataList[i].Position.y + 0.5f;
                newData.Matrix.c3.z = enemyDataList[i].Position.z;
            }
            else
            {
                newData.Matrix.c3.x = currentPos.x;
                newData.Matrix.c3.y = currentPos.y;
                newData.Matrix.c3.z = currentPos.z;
            }
            

            // --- Animation Update (Simplified: Increment frame by 1) ---
            // Calculate frame increment based on speed and time
            float frameIncrement = playbackFramesPerSecond * deltaTime;
            float newFrame = newData.AnimationFrame + frameIncrement;

            // Loop animation frame number
            if (numFrames > 0)
            {
                newFrame = math.fmod(newFrame, (float)numFrames);
                if (newFrame < 0) newFrame += numFrames;
            }
            else
            {
                newFrame = 0; // Default to frame 0 if no frames exist
            }
            newData.AnimationFrame = newFrame; // Store updated frame

            // Write back the updated instance data
            instanceData[i] = newData;
        }
    }

    bool ValidateSetup()
    {
        if (mesh == null)
        {
            Debug.LogError("Mesh property is not set.");
            return false;
        }
        if (material == null)
        {
            Debug.LogError("Material property is not set.");
            return false;
        }
        if (SystemInfo.supportsComputeShaders == false)
        {
            Debug.LogError("Compute shaders are not supported on this platform/hardware.");
            return false;
        }
        return true;

    }

    void LoadAndPrepareFrameData()
    {
        LoadData(); // Load from binary file based on dataRef

        // Validate and store counts after loading
        if (loadedFrames != null && loadedFrames.Length > 0)
        {
            _frameCountActual = loadedFrames.Length;
            if (loadedFrames[0] != null && loadedFrames[0].Length > 0)
            {
                _vertexCountPerFrame = loadedFrames[0].Length;
                // Basic validation: Ensure all frames have the same vertex count
                for (int i = 1; i < _frameCountActual; i++)
                {
                    if (loadedFrames[i] == null || loadedFrames[i].Length != _vertexCountPerFrame)
                    {
                        Debug.LogError($"Frame {i} has different vertex count ({loadedFrames[i]?.Length ?? -1}) or is null! Expected {_vertexCountPerFrame}.");
                        _frameCountActual = 0; _vertexCountPerFrame = 0; // Invalidate data
                        loadedFrames = null; // Clear invalid data
                        return;
                    }
                }
                Debug.Log($"Loaded {_frameCountActual} frames with {_vertexCountPerFrame} vertices per frame.");
            }
            else
            {
                Debug.LogError("First frame of loaded animation data is null or empty!");
                _frameCountActual = 0; _vertexCountPerFrame = 0;
                loadedFrames = null;
            }
        }
        else
        {
            Debug.LogWarning("loadedFrames is null or empty after LoadData attempt.");
            _frameCountActual = 0; _vertexCountPerFrame = 0;
        }
    }

    void LoadData()
    {
        if (dataRef == null) { Debug.LogError("Data Reference (SO) is not assigned!"); return; }

        // Construct the full path using StreamingAssets
        string fullPath = Path.Combine(Application.streamingAssetsPath, dataRef.binaryDataPath);

        if (!File.Exists(fullPath))
        {
            Debug.LogError($"Binary data file not found at: {fullPath}");
            loadedFrames = null; // Ensure loadedFrames is null if file doesn't exist
            return;
        }

        // Pre-allocate the outer array based on expected frame count
        loadedFrames = new Vector3[dataRef.frameCount][];

        try
        {
            using (FileStream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                // Read metadata from the file header
                int fileVertexCount = reader.ReadInt32();
                int fileFrameCount = reader.ReadInt32();
                int fileFrameRate = reader.ReadInt32(); // Read frame rate even if not directly used here

                // Validate against ScriptableObject metadata
                if (fileVertexCount != dataRef.vertexCount || fileFrameCount != dataRef.frameCount /*|| fileFrameRate != dataRef.frameRate*/) // Optional: Validate framerate too
                {
                    Debug.LogError($"Metadata mismatch between file header ({fileVertexCount} verts, {fileFrameCount} frames) and ScriptableObject ({dataRef.vertexCount} verts, {dataRef.frameCount} frames)! Halting load.");
                    loadedFrames = null; // Discard potentially mismatched data
                    return;
                }

                // Read vertex data frame by frame
                for (int f = 0; f < dataRef.frameCount; f++)
                {
                    Vector3[] vertices = new Vector3[dataRef.vertexCount];
                    for (int v = 0; v < dataRef.vertexCount; v++)
                    {
                        // Read X, Y, Z for each vertex
                        vertices[v] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                    }
                    loadedFrames[f] = vertices; // Assign the loaded vertex array to the correct frame index
                }
            }
            Debug.Log($"Successfully loaded animation data from: {fullPath}");
        }
        catch (EndOfStreamException eof)
        {
            Debug.LogError($"Failed to read binary data: Reached end of stream prematurely. Is the file corrupted or metadata incorrect? {eof.Message}");
            loadedFrames = null; // Clear partial data on error
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to read binary data: {e.Message}");
            loadedFrames = null; // Clear partial data on error
        }
    }

    Vector3[] FlattenFrames(Vector3[][] frames)
    {
        if (frames == null || frames.Length == 0 || frames[0] == null || frames[0].Length == 0) return null;
        int frameCount = frames.Length;
        int vertsPerFrame = frames[0].Length;
        if (vertsPerFrame == 0) return null; // Avoid division by zero or zero-length buffer
        Vector3[] flatData = new Vector3[frameCount * vertsPerFrame];
        for (int f = 0; f < frameCount; f++)
        {
            if (frames[f] != null && frames[f].Length == vertsPerFrame)
            { // Check each frame
                System.Array.Copy(frames[f], 0, flatData, f * vertsPerFrame, vertsPerFrame);
            }
            else
            {
                Debug.LogError($"Frame {f} is null or has incorrect vertex count ({frames[f]?.Length ?? -1} vs {vertsPerFrame}). Cannot flatten.");
                return null; // Stop flattening if data is inconsistent
            }
        }
        return flatData;
    }
}