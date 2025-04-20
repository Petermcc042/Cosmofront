using UnityEngine;

// This ScriptableObject stores metadata about the baked animation
// and the path to the binary file containing the actual vertex data.
[CreateAssetMenu(fileName = "NewBakedAnimRef", menuName = "Animation/Baked Data Reference")]
public class BakedDataReference : ScriptableObject
{
    [Header("Metadata")]
    public string clipName;
    public int vertexCount;
    public int frameCount;
    public int frameRate; // FPS the clip was baked at

    [Header("Data Location")]
    // Path to the binary file, RELATIVE to the StreamingAssets folder.
    public string binaryDataPath;

    [Header("Unique Enemy Data")]
    public Mesh mesh;
    public Material material;
    public int instanceCount = 4000;
}