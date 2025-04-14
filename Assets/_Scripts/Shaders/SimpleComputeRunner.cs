using UnityEngine;

public class SimpleComputeRunner : MonoBehaviour
{
    // Assign your Compute Shader asset in the Inspector
    public ComputeShader simpleComputeShader;

    // Store a reference to the buffer on the GPU
    private ComputeBuffer _resultBuffer;
    // Store a reference to the kernel function inside the shader
    private int _kernelIndex;

    // Our example data
    private float[] _data = new float[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 }; // 16 elements

    void Start()
    {
        if (simpleComputeShader == null)
        {
            Debug.LogError("Compute Shader not assigned!", this);
            return;
        }

        // --- 1. Setup ---

        // Create the ComputeBuffer on the GPU
        // Parameters:
        // - count: Number of elements in the buffer (must match data array size)
        // - stride: Size of ONE element in bytes (float is 4 bytes)
        int count = _data.Length;
        int stride = sizeof(float);
        _resultBuffer = new ComputeBuffer(count, stride);

        // Send our initial data from the CPU (_data array) to the GPU buffer (_resultBuffer)
        _resultBuffer.SetData(_data);

        // Find the index of the kernel function we want to run ("MultiplyByTwo")
        _kernelIndex = simpleComputeShader.FindKernel("MultiplyByTwo");

        // --- 2. Execution ---

        // Link the GPU buffer (_resultBuffer) to the variable named "ResultBuffer" inside the shader
        simpleComputeShader.SetBuffer(_kernelIndex, "ResultBuffer", _resultBuffer);

        // Calculate how many thread groups we need to run.
        // Our shader uses [numthreads(8,1,1)], so each group handles 8 elements.
        // We need ceiling(data.Length / 8) groups.
        int threadGroupsX = Mathf.CeilToInt(count / 8.0f);
        if (threadGroupsX == 0) threadGroupsX = 1; // Need at least one group

        // Tell the GPU to run the kernel!
        // Parameters:
        // - kernelIndex: Which kernel function to run.
        // - threadGroupsX, threadGroupsY, threadGroupsZ: How many groups to run in each dimension.
        simpleComputeShader.Dispatch(_kernelIndex, threadGroupsX, 1, 1);

        // --- 3. Get Results ---

        // Create a CPU array to receive the results back from the GPU
        float[] results = new float[count];

        // Copy the data FROM the GPU buffer (_resultBuffer) TO the CPU array (results)
        // This line WAITS for the GPU to finish its work before continuing.
        _resultBuffer.GetData(results);

        // --- 4. Use Results & Cleanup ---

        // Print the results
        Debug.Log("Original Data: " + string.Join(", ", _data));
        Debug.Log("GPU Results:   " + string.Join(", ", results));

        // IMPORTANT: Release the GPU buffer when you're done with it to free up memory
        // Usually done in OnDestroy or OnDisable
        // _resultBuffer.Release(); // Moved to OnDestroy
    }

    void OnDestroy()
    {
        // Ensure the buffer is released when the script is destroyed or the game stops
        if (_resultBuffer != null)
        {
            _resultBuffer.Release();
            _resultBuffer = null; // Good practice to null the reference
        }
    }
}