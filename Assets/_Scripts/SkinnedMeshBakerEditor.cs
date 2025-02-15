using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class SkinnedMeshBakerEditor : EditorWindow
{
    public SkinnedMeshRenderer skinnedMeshRenderer;
    public AnimationClip animationClip;
    public int frameRate = 30; // FPS for baking
    private int frameCount;
    private Vector3 positionMin;
    private Vector3 positionMax;

    [MenuItem("Tools/Bake Skinned Mesh Animation")]
    public static void ShowWindow()
    {
        GetWindow<SkinnedMeshBakerEditor>("Bake Skinned Animation");
    }

    void OnGUI()
    {
        GUILayout.Label("Bake Skinned Mesh Animation", EditorStyles.boldLabel);

        skinnedMeshRenderer = (SkinnedMeshRenderer)EditorGUILayout.ObjectField("Skinned Mesh", skinnedMeshRenderer, typeof(SkinnedMeshRenderer), true);
        animationClip = (AnimationClip)EditorGUILayout.ObjectField("Animation Clip", animationClip, typeof(AnimationClip), true);
        frameRate = EditorGUILayout.IntField("Frame Rate", frameRate);

        if (GUILayout.Button("Bake Animation"))
        {
            if (skinnedMeshRenderer && animationClip)
            {
                BakeAnimation();
            }
            else
            {
                Debug.LogError("Please assign a SkinnedMeshRenderer and an AnimationClip.");
            }
        }
    }

    void BakeAnimation()
    {
        Mesh bakedMesh = new Mesh();
        float animLength = animationClip.length;
        frameCount = Mathf.CeilToInt(animLength * frameRate);
        int vertexCount = skinnedMeshRenderer.sharedMesh.vertexCount;

        List<Vector3[]> vertexFrames = new List<Vector3[]>();

        // Find position bounds across all frames
        positionMin = Vector3.positiveInfinity;
        positionMax = Vector3.negativeInfinity;

        AnimationMode.StartAnimationMode();
        GameObject tempGO = Instantiate(skinnedMeshRenderer.gameObject);
        SkinnedMeshRenderer tempRenderer = tempGO.GetComponent<SkinnedMeshRenderer>();

        // First pass: find bounds
        for (int i = 0; i < frameCount; i++)
        {
            float sampleTime = (i / (float)frameRate) * animLength;
            AnimationMode.SampleAnimationClip(tempGO, animationClip, sampleTime);
            tempRenderer.BakeMesh(bakedMesh);

            foreach (Vector3 vertex in bakedMesh.vertices)
            {
                positionMin = Vector3.Min(positionMin, vertex);
                positionMax = Vector3.Max(positionMax, vertex);
            }
        }

        // Second pass: store normalized vertices
        for (int i = 0; i < frameCount; i++)
        {
            float sampleTime = (i / (float)frameRate) * animLength;
            AnimationMode.SampleAnimationClip(tempGO, animationClip, sampleTime);
            tempRenderer.BakeMesh(bakedMesh);
            vertexFrames.Add(bakedMesh.vertices);
        }

        AnimationMode.StopAnimationMode();
        DestroyImmediate(tempGO);

        SaveAsTexture(vertexFrames, vertexCount);
    }

    void SaveAsTexture(List<Vector3[]> vertexFrames, int vertexCount)
    {
        Texture2D texture = new Texture2D(vertexCount, frameCount + 1, TextureFormat.RGBAFloat, false);
        Vector3 range = positionMax - positionMin;

        // Store bounds in the last row of the texture
        for (int x = 0; x < vertexCount; x++)
        {
            // Store min in first third of pixels
            if (x == 0) texture.SetPixel(x, frameCount, new Color(positionMin.x, positionMin.y, positionMin.z, 1));
            // Store max in second third of pixels
            if (x == 1) texture.SetPixel(x, frameCount, new Color(positionMax.x, positionMax.y, positionMax.z, 1));
        }

        for (int y = 0; y < frameCount; y++)
        {
            for (int x = 0; x < vertexCount; x++)
            {
                Vector3 pos = vertexFrames[y][x];
                // Normalize each component separately
                float normalizedX = (pos.x - positionMin.x) / range.x;
                float normalizedY = (pos.y - positionMin.y) / range.y;
                float normalizedZ = (pos.z - positionMin.z) / range.z;
                texture.SetPixel(x, y, new Color(normalizedX, normalizedY, normalizedZ, 1));
            }
        }

        texture.Apply();

        // Save the texture (same as before)
        string directoryPath = Application.dataPath + "/_BakedAnimations";
        if (!System.IO.Directory.Exists(directoryPath))
        {
            System.IO.Directory.CreateDirectory(directoryPath);
        }

        string path = directoryPath + "/BakedAnimation.png";
        byte[] bytes = texture.EncodeToPNG();
        System.IO.File.WriteAllBytes(path, bytes);

        AssetDatabase.Refresh();

        string assetPath = "Assets/_BakedAnimations/BakedAnimation.png";
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            importer.isReadable = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
        }

        Debug.Log("Saved baked animation texture to: " + path);
    }
}
