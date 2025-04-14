using UnityEngine;
using System.Collections.Generic;
using System.IO;

// No custom InstanceRenderData struct needed for this approach

public class InstancedRunner : MonoBehaviour // Keep your current script name
{
    [Header("Configuration")]
    public int numEnemies = 100; // *** SET the fixed number of enemies here ***
    public float placementRadius = 20.0f; // Radius to randomly place enemies within at start
    public Mesh sourceMesh;
    public Material instancedMaterial; // Should use a shader that accepts _AnimatedVertices buffer

    [Header("Animation Data")]
    public BakedDataReference dataRef;
    public ComputeShader multiFrameInterpolatorShader;
    public float framesPerSecond = 30.0f; // Ensure this matches baked data or desired playback

    // --- Compute Shader Variables ---
    private Vector3[][] loadedFrames;
    private ComputeBuffer _allFramesVertexBuffer;
    private ComputeBuffer _outputVertexBuffer;
    private int _computeKernelIndex = -1;
    private int _vertexCountPerFrame = 0;
    private int _frameCountActual = 0;

    // --- Rendering Variables ---
    private Matrix4x4[] matrixArray; // Array to hold transforms for each instance
    private Vector3[] enemyPositions; // Store the fixed positions

    // --- Cached Shader Property ID ---
    private static readonly int AnimatedVerticesID = Shader.PropertyToID("_AnimatedVertices");

    void Start()
    {
        // --- Essential Checks ---
        if (!SystemInfo.supportsComputeShaders)
        {
            Debug.LogError("Compute shaders are not supported on this system.");
            enabled = false; return;
        }
        if (multiFrameInterpolatorShader == null) { Debug.LogError("Compute Shader is not assigned."); enabled = false; return; }
        if (instancedMaterial == null) { Debug.LogError("Instanced Material is not assigned."); enabled = false; return; }
        if (sourceMesh == null) { Debug.LogError("Source Mesh is not assigned."); enabled = false; return; }
        if (dataRef == null) { Debug.LogError("Baked Data Reference (ScriptableObject) is not assigned."); enabled = false; return; }
        if (numEnemies <= 0) { Debug.LogWarning("numEnemies is zero or negative. No instances will be drawn."); enabled = false; return; }


        // --- Initialize Arrays for Fixed Number of Enemies ---
        matrixArray = new Matrix4x4[numEnemies];
        enemyPositions = new Vector3[numEnemies];

        // --- Load Animation Data ---
        LoadAndPrepareFrameData();
        if (_frameCountActual == 0 || _vertexCountPerFrame == 0)
        {
            Debug.LogError("Failed to load or prepare valid animation frame data.");
            enabled = false; return;
        }

        // --- Setup Compute Shader ---
        if (!SetupComputeShader())
        {
            enabled = false; return;
        }

        // --- Set Initial Fixed Positions ---
        InitializeEnemyPositions();

        Debug.Log($"SimpleFixedInstancedRunner initialized with {numEnemies} enemies.");
    }

    void InitializeEnemyPositions()
    {
        // Simple random placement within a circle on the XZ plane
        for (int i = 0; i < numEnemies; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float distance = Random.Range(0f, placementRadius); // Use square root for more even distribution if needed: Mathf.Sqrt(Random.value) * placementRadius;
            enemyPositions[i] = new Vector3(Mathf.Cos(angle) * distance, 0, Mathf.Sin(angle) * distance);

            // Initialize the matrix as well to avoid issues before first Update
            matrixArray[i] = Matrix4x4.TRS(enemyPositions[i], Quaternion.identity, Vector3.one);
        }
    }

    bool SetupComputeShader()
    {
        // (Setup compute shader - same as before)
        int strideVec3 = sizeof(float) * 3;
        Vector3[] flatVertexData = FlattenFrames(loadedFrames);
        if (flatVertexData == null) { Debug.LogError("Failed to flatten frame data"); return false; }
        _allFramesVertexBuffer?.Release(); 
        _outputVertexBuffer?.Release();

        _allFramesVertexBuffer = new ComputeBuffer(flatVertexData.Length, strideVec3);
        _outputVertexBuffer = new ComputeBuffer(_vertexCountPerFrame, strideVec3, ComputeBufferType.Default);

        _allFramesVertexBuffer.SetData(flatVertexData);
        _computeKernelIndex = multiFrameInterpolatorShader.FindKernel("InterpolateMultiFrame");
        if (_computeKernelIndex < 0) { Debug.LogError("Compute kernel not found."); return false; }
        multiFrameInterpolatorShader.SetBuffer(_computeKernelIndex, "AllFramesVertexBuffer", _allFramesVertexBuffer);
        multiFrameInterpolatorShader.SetBuffer(_computeKernelIndex, "OutputVertexBuffer", _outputVertexBuffer);
        multiFrameInterpolatorShader.SetFloat("FramesPerSecond", framesPerSecond);
        multiFrameInterpolatorShader.SetInt("FrameCount", _frameCountActual);
        multiFrameInterpolatorShader.SetInt("VertexCountPerFrame", _vertexCountPerFrame);
        return true;
    }

    void Update()
    {
        // Basic checks to ensure everything is ready
        if (_computeKernelIndex < 0 || _outputVertexBuffer == null || !instancedMaterial || !sourceMesh) return;

        // --- 1. Run Compute Shader to Update Animation ---
        multiFrameInterpolatorShader.SetFloat("AnimTime", Time.time);

        // Calculate thread groups needed
        // Ensure your compute shader kernel definition uses a sensible numthreads value (e.g., [numthreads(64, 1, 1)])
        int threadGroupsX = Mathf.CeilToInt(_vertexCountPerFrame / 64.0f);
        if (threadGroupsX > 0)
        {
            multiFrameInterpolatorShader.Dispatch(_computeKernelIndex, threadGroupsX, 1, 1);
        }

        // --- 2. Update Instance Transforms (Matrices) ---
        for (int i = 0; i < numEnemies; i++)
        {
            Vector3 position = enemyPositions[i]; // Use the fixed position
            // Optional: Add simple rotation or keep static
            Quaternion rotation = Quaternion.Euler(0, (Time.time * 30f + i * 15f) % 360, 0); // Example simple rotation
            Vector3 scale = Vector3.one; // Assuming uniform scale

            matrixArray[i] = Matrix4x4.TRS(position, rotation, scale);
        }


        // --- 4. Draw All Instances ---
        Graphics.DrawMeshInstanced(
            sourceMesh,
            0, // submesh index
            instancedMaterial,
            matrixArray, // Pass the array of matrices
            numEnemies, // Draw the *fixed* number of enemies
            null, // MaterialPropertyBlock (optional, null if not needed)
            UnityEngine.Rendering.ShadowCastingMode.Off, // Cast shadows?
            false // Receive shadows?
        // Camera camera = null, // Optional: Cull based on specific camera
        // int layer = 0, // Optional: Render on specific layer
        );
    }

    void OnDestroy()
    {
        // --- Cleanup Compute Buffers ---
        _allFramesVertexBuffer?.Release();
        _outputVertexBuffer?.Release();
        _allFramesVertexBuffer = null;
        _outputVertexBuffer = null;

        // Nullify arrays to help GC
        matrixArray = null;
        enemyPositions = null;
        loadedFrames = null; // If large, good to nullify
    }

    // --- Helper Methods for Loading Animation Data ---
    // (These are kept largely the same as they handle loading/preparing the core animation)

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

} // End of class