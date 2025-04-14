using UnityEngine;
using System.Linq; // For Debug printing
using System.Collections.Generic; // If needed for frame loading
using System.IO;

public class MultiFrameRunner : MonoBehaviour
{
    public BakedDataReference dataRef; // Assign your BakedDataReference SO

    public ComputeShader multiFrameInterpolatorShader;
    public float framesPerSecond = 30.0f;

    // --- Multi-Frame Vertex Data ---
    // How you get this data depends on your baking process.
    // Option A: Reference your existing baked data structure
    private Vector3[][] loadedFrames; // Assign or load this! [frame][vertex]

    // Option B: Placeholder - Generate dummy data if you don't have baked data yet
    public int frameCount = 10; // Number of frames to generate
    public Mesh sourceMesh; // Assign a mesh (e.g., Cube) to generate data from

    // --- GPU Buffers ---
    private ComputeBuffer _allFramesVertexBuffer; // Single buffer for all vertex data
    private ComputeBuffer _outputVertexBuffer;    // Buffer for interpolated results

    private int _kernelIndex;
    private int _vertexCountPerFrame = 0;
    private int _frameCountActual = 0; // Actual number of frames loaded/generated

    void Start()
    {
        if (multiFrameInterpolatorShader == null) { /* Error */ return; }

        // --- 1. Load or Generate Multi-Frame Data ---
        LoadAndPrepareFrameData(); // Call helper method

        if (_frameCountActual == 0 || _vertexCountPerFrame == 0)
        {
            Debug.LogError("Failed to load or generate frame data.", this);
            return;
        }

        // --- 2. Setup GPU Buffers ---
        int stride = sizeof(float) * 3; // Stride for Vector3

        // Flatten the multi-frame data into a single array
        Vector3[] flatVertexData = FlattenFrames(loadedFrames);

        if (flatVertexData == null) { Debug.LogError("Failed to flatten frame data"); return; }

        // Create the large input buffer and the output buffer
        _allFramesVertexBuffer = new ComputeBuffer(flatVertexData.Length, stride);
        _outputVertexBuffer = new ComputeBuffer(_vertexCountPerFrame, stride); // Output is size of one frame

        // Send flattened data to the GPU
        _allFramesVertexBuffer.SetData(flatVertexData);

        // --- 3. Prepare Shader ---
        _kernelIndex = multiFrameInterpolatorShader.FindKernel("InterpolateMultiFrame");

        // Link buffers
        multiFrameInterpolatorShader.SetBuffer(_kernelIndex, "AllFramesVertexBuffer", _allFramesVertexBuffer);
        multiFrameInterpolatorShader.SetBuffer(_kernelIndex, "OutputVertexBuffer", _outputVertexBuffer);

        // Set uniforms
        multiFrameInterpolatorShader.SetFloat("FramesPerSecond", framesPerSecond);
        multiFrameInterpolatorShader.SetInt("FrameCount", _frameCountActual);
        multiFrameInterpolatorShader.SetInt("VertexCountPerFrame", _vertexCountPerFrame);
    }

    void Update()
    {
        if (_outputVertexBuffer == null || _kernelIndex < 0) return;

        // --- 4. Execute Shader Every Frame ---
        multiFrameInterpolatorShader.SetFloat("AnimTime", Time.time);

        // Calculate thread groups needed (based on vertices PER FRAME)
        int threadGroupsX = Mathf.CeilToInt(_vertexCountPerFrame / 64.0f);
        if (threadGroupsX == 0) threadGroupsX = 1;

        // Dispatch
        multiFrameInterpolatorShader.Dispatch(_kernelIndex, threadGroupsX, 1, 1);

        // --- 5. Get Results (FOR DEBUGGING ONLY) ---
        Vector3[] results = new Vector3[_vertexCountPerFrame];
        _outputVertexBuffer.GetData(results);

        if (results.Length > 0)
        {
            Debug.Log($"Frame {Time.frameCount}: Vertex 0 Position = {results[0]}");
        }
    }

    // --- Helper Methods ---

    void LoadAndPrepareFrameData()
    {
        // --- Replace this section with your actual frame loading logic ---

        // Option A: Use previously baked 'loadedFrames' (if you have it)
        // Example: Assume 'MyAnimationBaker.LoadBakedData()' returns Vector3[][]
        // loadedFrames = MyAnimationBaker.LoadBakedData("your_baked_file.bin");
        // if (loadedFrames == null || loadedFrames.Length == 0) { return; }

        // Option B: Generate placeholder data (REMOVE THIS if using Option A)
        if (sourceMesh == null) { Debug.LogError("Source Mesh not assigned for data generation!"); return; }
        //Debug.LogWarning("Generating placeholder animation data procedurally.");
        //GeneratePlaceholderFrames(frameCount);
        // --- End Placeholder Generation ---

        LoadData();


        // Validate and store counts
        if (loadedFrames != null && loadedFrames.Length > 0)
        {
            _frameCountActual = loadedFrames.Length;
            _vertexCountPerFrame = loadedFrames[0].Length; // Assume all frames have same vertex count
            // Basic validation
            for (int i = 1; i < _frameCountActual; i++)
            {
                if (loadedFrames[i].Length != _vertexCountPerFrame)
                {
                    Debug.LogError($"Frame {i} has different vertex count!");
                    _frameCountActual = 0; _vertexCountPerFrame = 0; // Invalidate data
                    return;
                }
            }
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
        { Debug.LogError($"Failed to read binary data: {e.Message}"); loadedFrames = null; }
    }

    // Example placeholder data generator (replace with your actual loading)
    void GeneratePlaceholderFrames(int numFrames)
    {
        loadedFrames = new Vector3[numFrames][];
        Vector3[] baseVertices = sourceMesh.vertices;
        _vertexCountPerFrame = baseVertices.Length;

        for (int f = 0; f < numFrames; f++)
        {
            loadedFrames[f] = new Vector3[_vertexCountPerFrame];
            float angle = (f / (float)numFrames) * 360.0f * 2.0f; // Example: Rotate twice
            Quaternion rot = Quaternion.Euler(0, angle, 0);
            float scale = 1.0f + Mathf.Sin((f / (float)numFrames) * Mathf.PI * 2.0f) * 0.2f; // Example: pulse scale

            for (int v = 0; v < _vertexCountPerFrame; v++)
            {
                loadedFrames[f][v] = (rot * baseVertices[v]) * scale;
            }
        }
    }


    Vector3[] FlattenFrames(Vector3[][] frames)
    {
        if (frames == null || frames.Length == 0) return null;
        int frameCount = frames.Length;
        int vertsPerFrame = frames[0].Length;
        Vector3[] flatData = new Vector3[frameCount * vertsPerFrame];
        for (int f = 0; f < frameCount; f++)
        {
            System.Array.Copy(frames[f], 0, flatData, f * vertsPerFrame, vertsPerFrame);
        }
        return flatData;
    }


    void OnDestroy()
    {
        // Release buffers
        _allFramesVertexBuffer?.Release();
        _outputVertexBuffer?.Release();
        _allFramesVertexBuffer = null;
        _outputVertexBuffer = null;
    }
}