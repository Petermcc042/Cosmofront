using UnityEngine;
using System.Collections.Generic; // For List
using System.IO;

// Struct to hold per-instance data, must match layout in the rendering shader
[System.Serializable]
public struct InstanceRenderData
{
    public Vector3 position;
    public Quaternion rotation; // Use Quaternion in C#, send as float4 (xyzw)
}

public class InstancedAnimationRunner : MonoBehaviour
{
    public BakedDataReference dataRef; // Assign your BakedDataReference SO

    // --- Compute Shader Vars ---
    public ComputeShader multiFrameInterpolatorShader;
    public float framesPerSecond = 30.0f;
    private Vector3[][] loadedFrames;
    public Mesh sourceMesh;
    private ComputeBuffer _allFramesVertexBuffer; // Compute Input
    private ComputeBuffer _outputVertexBuffer;    // Compute Output & Render Input
    private int _computeKernelIndex = -1;
    private int _vertexCountPerFrame = 0;
    private int _frameCountActual = 0;

    // --- Rendering Vars ---
    // *** Ensure this uses the HLSL Shader: Custom/InstancedAnimatedHLSL ***
    public Material instancedMaterial;
    public int maxInstances = 10000;
    private ComputeBuffer _instanceDataBuffer; // Instance positions/rotations
    private ComputeBuffer _argsBuffer;
    private uint[] _args = new uint[5] { 0, 0, 0, 0, 0 };

    // --- Dynamic Enemy Data ---
    public List<Vector3> currentEnemyPositions = new List<Vector3>();
    private List<InstanceRenderData> _instanceDataList = new List<InstanceRenderData>();

    // *** ADDED: MaterialPropertyBlock ***
    private MaterialPropertyBlock _propertyBlock;
    // *** ADDED: Cached Shader Property IDs for performance ***
    private static readonly int AnimatedVerticesID = Shader.PropertyToID("_AnimatedVertices");
    private static readonly int PerInstanceDataID = Shader.PropertyToID("_PerInstanceData");


    void Start()
    {
        // (Error checks for compute support, assigned objects etc.)
        if (!SystemInfo.supportsComputeShaders) { Debug.LogError("Compute shaders not supported!"); enabled = false; return; }
        if (multiFrameInterpolatorShader == null) { Debug.LogError("Compute Shader not assigned!"); enabled = false; return; }
        if (instancedMaterial == null) { Debug.LogError("Instanced Material not assigned!"); enabled = false; return; }
        if (sourceMesh == null) { Debug.LogError("Source Mesh not assigned!"); enabled = false; return; }

        // *** ADDED: Initialize Property Block ***
        _propertyBlock = new MaterialPropertyBlock();

        // 1. Load Animation Data
        LoadAndPrepareFrameData();
        if (_frameCountActual == 0 || _vertexCountPerFrame == 0) { /* Error */ enabled = false; return; }

        // 2. Setup Compute Resources
        if (!SetupComputeShader()) { enabled = false; return; }

        // 3. Setup Rendering Resources
        if (!SetupRendering()) { enabled = false; return; }

        // 4. Add initial enemies
        AddInitialEnemiesForDebug(50);
    }

    bool SetupComputeShader()
    {
        int strideVec3 = sizeof(float) * 3;
        Vector3[] flatVertexData = FlattenFrames(loadedFrames);
        if (flatVertexData == null) { Debug.LogError("Failed to flatten frame data"); return false; }
        _allFramesVertexBuffer?.Release();
        _outputVertexBuffer?.Release();
        _allFramesVertexBuffer = new ComputeBuffer(flatVertexData.Length, strideVec3);
        // Make sure output buffer is structured if reading directly, Default is okay too usually.
        _outputVertexBuffer = new ComputeBuffer(_vertexCountPerFrame, strideVec3, ComputeBufferType.Default);
        _allFramesVertexBuffer.SetData(flatVertexData);
        _computeKernelIndex = multiFrameInterpolatorShader.FindKernel("InterpolateMultiFrame");
        if (_computeKernelIndex < 0) { Debug.LogError("Compute kernel 'InterpolateMultiFrame' not found."); return false; }
        multiFrameInterpolatorShader.SetBuffer(_computeKernelIndex, "AllFramesVertexBuffer", _allFramesVertexBuffer);
        multiFrameInterpolatorShader.SetBuffer(_computeKernelIndex, "OutputVertexBuffer", _outputVertexBuffer);
        multiFrameInterpolatorShader.SetFloat("FramesPerSecond", framesPerSecond);
        multiFrameInterpolatorShader.SetInt("FrameCount", _frameCountActual);
        multiFrameInterpolatorShader.SetInt("VertexCountPerFrame", _vertexCountPerFrame);
        return true;
    }

    bool SetupRendering() // Uses maxInstances
    {
        int instanceStride = (sizeof(float) * 3) + (sizeof(float) * 4); // Vector3 + Quaternion
        _instanceDataBuffer?.Release();
        _argsBuffer?.Release();
        _instanceDataBuffer = new ComputeBuffer(maxInstances, instanceStride);

        // *** REMOVED: SetBuffer calls on the material ***
        // instancedMaterial.SetBuffer("_AnimatedVertices", _outputVertexBuffer);
        // instancedMaterial.SetBuffer("_PerInstanceData", _instanceDataBuffer);
        // Debug.Log($"_PerInstanceData buffer bound to material..."); // Also remove related log

        // Setup indirect args buffer (same as before)
        _argsBuffer = new ComputeBuffer(1, _args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        if (sourceMesh.subMeshCount > 0)
        {
            _args[0] = sourceMesh.GetIndexCount(0);
            _args[2] = sourceMesh.GetIndexStart(0);
            _args[3] = sourceMesh.GetBaseVertex(0);
        }
        else { Debug.LogError("Source Mesh has no submeshes!"); return false; }
        _args[1] = 0; _args[4] = 0;
        _argsBuffer.SetData(_args);
        return true;
    }


    void Update()
    {
        if (!enabled || _argsBuffer == null || _computeKernelIndex < 0 || 
            _outputVertexBuffer == null || _instanceDataBuffer == null || _propertyBlock == null) return;

        UpdateEnemyPositionsForDebug(Time.deltaTime);

        // --- 1. Run Compute Shader ---
        multiFrameInterpolatorShader.SetFloat("AnimTime", Time.time);
        int threadGroupsX = Mathf.CeilToInt(_vertexCountPerFrame / 64.0f);
        if (threadGroupsX == 0 && _vertexCountPerFrame > 0) threadGroupsX = 1;
        else if (_vertexCountPerFrame == 0) threadGroupsX = 0;
        if (threadGroupsX > 0)
        {
            multiFrameInterpolatorShader.Dispatch(_computeKernelIndex, threadGroupsX, 1, 1);
        }


        // --- 2. Prepare and Upload Instance Data ---
        int currentInstanceCount = currentEnemyPositions.Count;
        if (currentInstanceCount > maxInstances) { /* Clamp/Warn */ currentInstanceCount = maxInstances; }

        _instanceDataList.Clear();
        for (int i = 0; i < currentInstanceCount; i++)
        { /* Populate _instanceDataList */
            _instanceDataList.Add(new InstanceRenderData
            {
                position = currentEnemyPositions[i],
                rotation = Quaternion.Euler(0, (Time.time * 20f + i * 10f) % 360, 0)
            });
        }
        if (currentInstanceCount > 0)
        {
            // Upload data TO THE BUFFER (this is still correct)
            _instanceDataBuffer.SetData(_instanceDataList, 0, 0, currentInstanceCount);
        }

        // Update the instance count in the args buffer (this is still correct)
        _args[1] = (uint)currentInstanceCount;
        _argsBuffer.SetData(_args);


        if (_outputVertexBuffer.IsValid() && _instanceDataBuffer.IsValid())
        {
            _propertyBlock.SetBuffer(AnimatedVerticesID, _outputVertexBuffer); // Use PropertyToID
            _propertyBlock.SetBuffer(PerInstanceDataID, _instanceDataBuffer);   // Use PropertyToID
                                                                                // You could set other per-draw properties here, like _Color:
                                                                                // _propertyBlock.SetColor(Shader.PropertyToID("_Color"), Color.red);
        }
        else
        {
            Debug.LogError("Compute Buffers invalid in Update, cannot set on PropertyBlock!");
            return; // Skip drawing if buffers are bad
        }


        // --- 3. Draw All Instances ---
        if (instancedMaterial != null && sourceMesh != null && currentInstanceCount > 0)
        {
            //Debug.Log("run instanced call"); // Keep if needed
            Graphics.DrawMeshInstancedIndirect(
                sourceMesh, 0, instancedMaterial,
                new Bounds(Vector3.zero, Vector3.one * 1000), // Adjust bounds
                _argsBuffer, 0,
                _propertyBlock, // *** CHANGED: Pass the property block here ***
                UnityEngine.Rendering.ShadowCastingMode.Off, false // Set shadows off
            );
        }
    }


    // --- DEBUGGING / EXAMPLE ---
    void AddInitialEnemiesForDebug(int count)
    {
        for (int i = 0; i < count; i++)
        {
            currentEnemyPositions.Add(new Vector3(Random.Range(-20f, 20f), 0, Random.Range(-20f, 20f)));
        }
    }

    // Simple example to simulate enemies moving, spawning, despawning
    void UpdateEnemyPositionsForDebug(float dt)
    {
        // Move existing enemies slightly
        for (int i = 0; i < currentEnemyPositions.Count; i++)
        {
            // Example: Move towards center
            Vector3 pos = currentEnemyPositions[i];
            Vector3 dir = (Vector3.zero - pos).normalized;
            pos += dir * dt * 3.0f; // Move speed 3 units/sec
            currentEnemyPositions[i] = pos;

            // Remove if they reach the center (example despawn)
            if (Vector3.Distance(pos, Vector3.zero) < 1.0f)
            {
                currentEnemyPositions.RemoveAt(i);
                i--; // Adjust index after removal
            }
        }

        // Occasionally spawn a new enemy (example spawn)
        if (Random.value < 0.05f && currentEnemyPositions.Count < maxInstances)
        { // Spawn chance ~3 times/sec
            currentEnemyPositions.Add(new Vector3(Random.Range(-30f, 30f), 0, Random.Range(-30f, 30f)));
        }
    }


    // --- Helpers (Keep LoadAndPrepareFrameData, GeneratePlaceholderFrames, FlattenFrames from previous script) ---
    void LoadAndPrepareFrameData()
    {
        LoadData();

        // Validate and store counts
        if (loadedFrames != null && loadedFrames.Length > 0)
        {
            _frameCountActual = loadedFrames.Length;
            if (loadedFrames[0] != null)
            { // Check if first frame is valid
                _vertexCountPerFrame = loadedFrames[0].Length; // Assume all frames have same vertex count
                // Basic validation
                for (int i = 1; i < _frameCountActual; i++)
                {
                    if (loadedFrames[i] == null || loadedFrames[i].Length != _vertexCountPerFrame)
                    {
                        Debug.LogError($"Frame {i} has different vertex count or is null!");
                        _frameCountActual = 0; _vertexCountPerFrame = 0; // Invalidate data
                        return;
                    }
                }
            }
            else
            {
                Debug.LogError("First frame of loadedFrames is null!");
                _frameCountActual = 0; _vertexCountPerFrame = 0;
            }
        }
        else
        {
            Debug.LogWarning("loadedFrames is null or empty after LoadAndPrepareFrameData attempt.");
            _frameCountActual = 0; _vertexCountPerFrame = 0;
        }
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
        { Debug.LogError($"Failed to read binary dataa: {e.Message}"); loadedFrames = null; }
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