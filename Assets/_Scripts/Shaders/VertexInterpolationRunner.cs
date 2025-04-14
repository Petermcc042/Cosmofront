using UnityEngine;
using System.Linq; // Used for Debug printing

public class VertexInterpolationRunner : MonoBehaviour
{
    public ComputeShader vertexInterpolatorShader;
    public float animationDuration = 2.0f; // How long the animation cycle takes (seconds)

    // --- Example Vertex Data ---
    // Replace this with actual mesh data later
    private Vector3[] _frame0Data;
    private Vector3[] _frame1Data;
    private const int _vertexCount = 4; // Example: A quad

    // --- GPU Buffers ---
    private ComputeBuffer _frame0Buffer;
    private ComputeBuffer _frame1Buffer;
    private ComputeBuffer _outputBuffer; // Buffer to receive interpolated results

    private int _kernelIndex;

    void Start()
    {
        if (vertexInterpolatorShader == null)
        {
            Debug.LogError("Vertex Interpolator Shader not assigned!", this);
            return;
        }

        // --- 1. Create Example Data ---
        // Create simple vertex data for a quad at the origin for frame 0
        _frame0Data = new Vector3[_vertexCount] {
            new Vector3(-0.5f, -0.5f, 0), // Bottom-left
            new Vector3( 0.5f, -0.5f, 0), // Bottom-right
            new Vector3(-0.5f,  0.5f, 0), // Top-left
            new Vector3( 0.5f,  0.5f, 0)  // Top-right
        };

        // Create modified vertex data for frame 1 (e.g., stretched quad)
        _frame1Data = new Vector3[_vertexCount] {
            new Vector3(-1.0f, -0.5f, 0), // Bottom-left (moved left)
            new Vector3( 1.0f, -0.5f, 0), // Bottom-right (moved right)
            new Vector3(-0.5f,  1.0f, 0), // Top-left (moved up)
            new Vector3( 0.5f,  1.0f, 0)  // Top-right (moved up)
        };


        // --- 2. Setup GPU Buffers ---
        // Stride is size of one Vector3 element (3 floats * 4 bytes/float = 12 bytes)
        int stride = sizeof(float) * 3;

        // Create buffers on the GPU
        _frame0Buffer = new ComputeBuffer(_vertexCount, stride);
        _frame1Buffer = new ComputeBuffer(_vertexCount, stride);
        _outputBuffer = new ComputeBuffer(_vertexCount, stride); // Output buffer

        // Send initial data to GPU buffers
        _frame0Buffer.SetData(_frame0Data);
        _frame1Buffer.SetData(_frame1Data);
        // Output buffer doesn't need initial data set

        // --- 3. Prepare Shader ---
        _kernelIndex = vertexInterpolatorShader.FindKernel("InterpolateVertices");

        // Link the GPU buffers to the variable names in the shader
        vertexInterpolatorShader.SetBuffer(_kernelIndex, "Frame0Vertices", _frame0Buffer);
        vertexInterpolatorShader.SetBuffer(_kernelIndex, "Frame1Vertices", _frame1Buffer);
        vertexInterpolatorShader.SetBuffer(_kernelIndex, "OutputVertices", _outputBuffer); // Link the output buffer

        // Set the constant animation duration
        vertexInterpolatorShader.SetFloat("AnimDuration", animationDuration);
    }

    void Update()
    {
        if (_outputBuffer == null) return; // Don't run if setup failed

        // --- 4. Execute Shader Every Frame ---

        // Set the current animation time (loops via frac() in shader)
        vertexInterpolatorShader.SetFloat("AnimTime", Time.time);

        // Calculate thread groups needed
        // Shader uses [numthreads(64, 1, 1)], so each group handles 64 vertices.
        int threadGroupsX = Mathf.CeilToInt(_vertexCount / 64.0f);
        if (threadGroupsX == 0) threadGroupsX = 1;

        // Dispatch the compute shader
        vertexInterpolatorShader.Dispatch(_kernelIndex, threadGroupsX, 1, 1);

        // --- 5. Get Results (FOR DEBUGGING ONLY) ---
        // Reading data back every frame is SLOW and not for production rendering.
        // We do it here just to verify the compute shader is working.
        Vector3[] results = new Vector3[_vertexCount];
        _outputBuffer.GetData(results);

        // Print the first vertex's position to the console to see it change
        if (results.Length > 0)
        {
            Debug.Log($"Frame {Time.frameCount}: Vertex 0 Position = {results[0]}");
        }
    }

    void OnDestroy()
    {
        // Release buffers when done
        _frame0Buffer?.Release();
        _frame1Buffer?.Release();
        _outputBuffer?.Release();

        _frame0Buffer = null;
        _frame1Buffer = null;
        _outputBuffer = null;
    }
}