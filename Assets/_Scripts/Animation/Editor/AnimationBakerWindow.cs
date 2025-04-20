using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO; 
using System;

public class BakedClipDataRuntime // Renamed slightly to avoid confusion
{
    public string clipName;
    public int vertexCount;
    public int frameRate;
    public int frameCount;
    public List<Vector3[]> frameVertices = new List<Vector3[]>();
}


public class AnimationBakerWindow : EditorWindow
{
    private GameObject sourcePrefab;
    public AnimationClip clipToBake;
    private int fps = 30;
    private DefaultAsset outputFolder = null; // Folder in Assets/ to save the SO reference
    private string assetName = "BakedAnim"; // Base name for SO and binary subfolder

    [MenuItem("Tools/Animation Baker (Single Clip to Binary)")] // Updated Menu Item
    public static void ShowWindow()
    {
        GetWindow<AnimationBakerWindow>("Animation Baker (Binary)");
    }

    void OnGUI()
    {
        GUILayout.Label("Single Animation Baking Setup (Output to Binary)", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        sourcePrefab = (GameObject)EditorGUILayout.ObjectField("Source Prefab", sourcePrefab, typeof(GameObject), false);
        clipToBake = (AnimationClip)EditorGUILayout.ObjectField("Animation Clip", clipToBake, typeof(AnimationClip), false);
        fps = EditorGUILayout.IntField("Sample FPS", fps);
        // Output Folder is now where the Reference SO Asset will be saved
        outputFolder = (DefaultAsset)EditorGUILayout.ObjectField("Output Ref Folder (Assets/)", outputFolder, typeof(DefaultAsset), false);
        // Asset Name is used for the SO and the subfolder in StreamingAssets
        assetName = EditorGUILayout.TextField("Output Base Name", assetName);

        EditorGUILayout.Space();

        // --- Validation ---
        bool isValid = true;
        Animator prefabAnimator = null;

        if (sourcePrefab == null) { /* ... existing validation ... */ isValid = false; }
        else
        {
            if (sourcePrefab.GetComponentInChildren<SkinnedMeshRenderer>() == null) { /* ... */ isValid = false; }
            prefabAnimator = sourcePrefab.GetComponentInChildren<Animator>();
            if (prefabAnimator == null) { /* ... */ isValid = false; }
        }
        if (clipToBake == null) { /* ... */ isValid = false; }

        // Avatar Validation for Humanoid Clips
        if (isValid && clipToBake != null && prefabAnimator != null)
        {
            if (clipToBake.isHumanMotion && prefabAnimator.avatar == null)
            {
                EditorGUILayout.HelpBox("Clip is Humanoid, but Prefab's Animator is missing Avatar!", MessageType.Error);
                isValid = false;
            }
        }
        if (fps <= 0) { /* ... */ isValid = false; }
        if (outputFolder == null) { /* ... */ isValid = false; }
        else { /* ... existing folder validation (must be in Assets/) ... */ }
        if (string.IsNullOrEmpty(assetName)) { /* ... */ isValid = false; }
        // --- End Validation ---

        GUI.enabled = isValid;

        if (GUILayout.Button($"Bake '{(clipToBake != null ? clipToBake.name : "...")}' to Binary"))
        {
            if (ValidateOutputPath(out string folderPathForSO)) // Path for the SO file
            {
                // Pass the base name for SO and binary subfolder
                BakeSingleAnimationToBinary(folderPathForSO, assetName);
            }
        }
        GUI.enabled = true;
    }

    bool ValidateOutputPath(out string folderPath) // Validates path for the SO file
    {
        folderPath = null;
        if (outputFolder == null) return false;
        folderPath = AssetDatabase.GetAssetPath(outputFolder);
        // --- Keep existing checks: !string.IsNullOrEmpty, Directory.Exists, folderPath.StartsWith("Assets") ---
        if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath)) { /* Error Dialog */ return false; }
        if (!folderPath.StartsWith("Assets")) { /* Error Dialog */ return false; }
        return true;
    }


    void BakeSingleAnimationToBinary(string folderPathForSO, string baseName)
    {
        GameObject instance = null;
        SkinnedMeshRenderer smr = null;
        BakedClipDataRuntime clipData = null; // Use the in-memory class

        if (clipToBake == null) { /* ... error log ... */ return; }

        EditorUtility.DisplayProgressBar($"Baking '{clipToBake.name}' to Binary", "Initializing...", 0f);

        try
        {
            instance = Instantiate(sourcePrefab);
            instance.hideFlags = HideFlags.HideAndDontSave;
            Animator animator = instance.GetComponentInChildren<Animator>();
            smr = instance.GetComponentInChildren<SkinnedMeshRenderer>();

            // --- Existing checks for components and avatar ---
            if (animator == null || smr == null || smr.sharedMesh == null) { throw new InvalidOperationException(/*...*/); }
            if (clipToBake.isHumanMotion && animator.avatar == null) { throw new InvalidOperationException(/*...*/); }
            int vertexCount = smr.sharedMesh.vertexCount;
            if (vertexCount == 0) { throw new InvalidOperationException(/*...*/); }
            // --- End Checks ---

            // Prepare in-memory data holder
            clipData = new BakedClipDataRuntime();
            clipData.clipName = clipToBake.name;
            clipData.vertexCount = vertexCount;
            clipData.frameRate = fps;

            float duration = clipToBake.length;
            int frameCount = Mathf.Max(1, Mathf.RoundToInt(duration * fps));
            clipData.frameCount = frameCount;

            // --- Baking Loop (same as before, populates clipData.frameVertices) ---
            for (int frame = 0; frame < frameCount; frame++)
            {
                float progress = (float)frame / frameCount;
                if (EditorUtility.DisplayCancelableProgressBar($"Baking '{clipToBake.name}'", $"Sampling Frame {frame + 1} / {frameCount}", progress))
                {
                    throw new OperationCanceledException("Baking cancelled by user.");
                }

                float time = (frameCount <= 1) ? 0f : (float)frame / (frameCount - 1) * duration;

                // Sample animation
                clipToBake.SampleAnimation(instance, time);

                // Bake mesh
                Mesh bakedMesh = new Mesh { name = $"TempBake_{clipToBake.name}_Frame{frame}" };
                smr.BakeMesh(bakedMesh, true);

                // Validate baked mesh vertices (same checks as before)
                if (bakedMesh.vertexCount != vertexCount) { /* Log Warning */ }
                if (bakedMesh.vertices == null || bakedMesh.vertices.Length != vertexCount) { /* Log Error and Throw */ DestroyImmediate(bakedMesh); throw new Exception(/*...*/); }


                // Add vertices to in-memory list
                clipData.frameVertices.Add(bakedMesh.vertices);
                DestroyImmediate(bakedMesh);
            }
            // --- End Baking Loop ---

            // If loop finished, clipData.frameVertices should contain data
            if (clipData.frameVertices.Count != frameCount)
            {
                // This might happen if an exception occurred mid-loop but wasn't re-thrown properly
                throw new Exception($"Internal error: Expected {frameCount} frames, but only baked {clipData.frameVertices.Count}.");
            }

            EditorUtility.DisplayProgressBar($"Saving '{clipToBake.name}'", "Writing binary file...", 0.9f);

            // --- NEW: Save data to binary file and create SO reference ---
            CreateBinaryDataFile(clipData, folderPathForSO, baseName);
            // --- End NEW ---

        }
        catch (OperationCanceledException) { Debug.Log($"Baking cancelled: {clipToBake?.name ?? "Unknown"}"); }
        catch (System.Exception e) { Debug.LogError($"Baking failed: {clipToBake?.name ?? "Unknown"}: {e.Message}\n{e.StackTrace}"); EditorUtility.DisplayDialog("Error","test","Ok"); }
        finally
        {
            if (instance != null) { DestroyImmediate(instance); }
            EditorUtility.ClearProgressBar();
        }
    }

    // New function to handle binary file writing and SO creation
    void CreateBinaryDataFile(BakedClipDataRuntime clipData, string folderPathForSO, string baseName)
    {
        // --- 1. Define Paths ---
        string sanitizedBaseName = string.Join("_", baseName.Split(Path.GetInvalidFileNameChars())).Replace(" ", "_");
        string sanitizedClipName = string.Join("_", clipData.clipName.Split(Path.GetInvalidFileNameChars())).Replace(" ", "_");

        // SO Asset Path (in Assets/)
        string soAssetName = $"{sanitizedBaseName}_{sanitizedClipName}_Ref";
        string soAssetPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(folderPathForSO, soAssetName + ".asset"));

        // Binary File Path (relative to StreamingAssets)
        string relativeBinarySubDir = $"{sanitizedBaseName}_{sanitizedClipName}_Data"; // Subfolder name under StreamingAssets
        string binaryFileName = "vertexData.bin";
        string relativeBinaryPath = Path.Combine(relativeBinarySubDir, binaryFileName); // Path stored in SO

        // Full paths for directory creation and writing
        string streamingAssetsPath = Application.streamingAssetsPath; // Platform-correct path to StreamingAssets
        string fullBinaryDirPath = Path.Combine(streamingAssetsPath, relativeBinarySubDir);
        string fullBinaryPath = Path.Combine(fullBinaryDirPath, binaryFileName);

        // --- 2. Ensure Directories Exist ---
        // Ensure StreamingAssets exists
        if (!Directory.Exists(streamingAssetsPath))
        {
            Directory.CreateDirectory(streamingAssetsPath);
            AssetDatabase.Refresh(); // Tell Unity StreamingAssets was created
            Debug.Log("Created StreamingAssets folder.");
        }
        // Ensure the subdirectory for our binary file exists
        if (!Directory.Exists(fullBinaryDirPath))
        {
            Directory.CreateDirectory(fullBinaryDirPath);
            // No AssetDatabase.Refresh needed here as it's outside Assets/ technically
            Debug.Log($"Created binary data directory: {fullBinaryDirPath}");
        }


        // --- 3. Write Binary File ---
        Debug.Log($"[Baker] Writing binary data to: {fullBinaryPath}");
        try
        {
            using (FileStream stream = new FileStream(fullBinaryPath, FileMode.Create, FileAccess.Write)) // FileMode.Create will overwrite if exists
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                // Write Header (optional but recommended)
                writer.Write(clipData.vertexCount); // int
                writer.Write(clipData.frameCount); // int
                writer.Write(clipData.frameRate); // int
                Debug.Log($"[Baker] Binary Header: Verts={clipData.vertexCount}, Frames={clipData.frameCount}, FPS={clipData.frameRate}");


                // Write Vertex Data (Frame by Frame)
                for (int f = 0; f < clipData.frameCount; f++)
                {
                    Vector3[] vertices = clipData.frameVertices[f];
                    if (vertices.Length != clipData.vertexCount)
                    {
                        // Safety check - should have been caught earlier
                        throw new IOException($"Vertex count mismatch in frame data buffer for frame {f}. Expected {clipData.vertexCount}, got {vertices.Length}.");
                    }
                    for (int v = 0; v < clipData.vertexCount; v++)
                    {
                        writer.Write(vertices[v].x); // float
                        writer.Write(vertices[v].y); // float
                        writer.Write(vertices[v].z); // float
                    }
                }
                writer.Flush(); // Ensure all data is written to the stream
                Debug.Log($"[Baker] Finished writing {clipData.frameCount} frames of vertex data.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[Baker] Failed to write binary file '{fullBinaryPath}': {e.Message}\n{e.StackTrace}");
            // Attempt to delete potentially corrupt partial file
            if (File.Exists(fullBinaryPath)) { try { File.Delete(fullBinaryPath); } catch { } }
            throw; // Re-throw the exception to be caught by the main handler
        }


        // --- 4. Create and Save Metadata SO Asset ---
        Debug.Log($"[Baker] Creating reference SO asset at: {soAssetPath}");
        BakedDataReference dataRefSO = CreateInstance<BakedDataReference>();

        // Populate SO fields
        dataRefSO.clipName = clipData.clipName;
        dataRefSO.vertexCount = clipData.vertexCount;
        dataRefSO.frameCount = clipData.frameCount;
        dataRefSO.frameRate = clipData.frameRate;
        dataRefSO.binaryDataPath = relativeBinaryPath; // Store the path RELATIVE to StreamingAssets

        // Save the SO asset
        AssetDatabase.CreateAsset(dataRefSO, soAssetPath);
        EditorUtility.SetDirty(dataRefSO); // Mark dirty
        AssetDatabase.SaveAssets(); // Save
        AssetDatabase.Refresh(); // Refresh view

        Debug.Log($"[Baker] Successfully saved reference SO and binary data for '{clipData.clipName}'. Binary Path (relative): {relativeBinaryPath}");

        // --- 5. Select the new SO Asset (Deferred) ---
        UnityEngine.Object assetToSelect = dataRefSO;
        EditorApplication.delayCall += () => {
            if (assetToSelect != null)
            {
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = assetToSelect;
            }
        };
    }
}