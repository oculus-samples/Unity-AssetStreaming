// Copyright (c) Meta Platforms, Inc. and affiliates.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

// Combines sublevels.
// 1. Collects the lightmaps from all LOD managers and combines them into a texture array.
// 2. Creates copies of the original meshes which store their lightmap index along with the lightmap uv coordinates. Replaces the meshes in the scene with the copies.
// 3. Enables the use of the batched lightmap on all materials used in the scene. Disables Unity lightmapping.
// 4. Creates scenes for each LOD level of each mesh. Needed for streaming.

[CustomEditor(typeof(SublevelCombiner))]
public class SublevelCombinerEditor : Editor
{
    private SublevelCombiner sublevelCombiner;

    public override void OnInspectorGUI()
    {
        sublevelCombiner = (SublevelCombiner)target;

        base.OnInspectorGUI();

        if(GUILayout.Button("Combine Sublevels"))
        {
            Execute();
        }
    }

    public void Execute()
    {
        LODManager[] lodManagers = FindObjectsOfType<LODManager>();
        LightmapInfo[] lightmapInfos = FindObjectsOfType<LightmapInfo>();

        // Collect the lightmaps and lightmap indices
        List<Texture2D> lightmaps = new List<Texture2D>();
        int[] lodManagerLightmapIndices = new int[lodManagers.Length];
        int[] otherLightmapIndices = new int[lightmapInfos.Length];

        for (int manager = 0; manager < lodManagers.Length; ++manager)
        {
            int lightmapIndex = -1;
            for (int i = 0; i < lightmaps.Count; ++i)
            {
                if (lightmaps[i] == lodManagers[manager].lightmapParameters.lightmap)
                {
                    lightmapIndex = i;
                    break;
                }
            }

            if (lightmapIndex == -1)
            {
                lightmapIndex = lightmaps.Count;
                lightmaps.Add(lodManagers[manager].lightmapParameters.lightmap);
            }
            lodManagerLightmapIndices[manager] = lightmapIndex;
        }

        for (int other = 0; other < lightmapInfos.Length; ++other)
        {
            int lightmapIndex = -1;
            for (int i = 0; i < lightmaps.Count; ++i)
            {
                if (lightmaps[i] == lightmapInfos[other].lightmap)
                {
                    lightmapIndex = i;
                    break;
                }
            }

            if (lightmapIndex == -1)
            {
                lightmapIndex = lightmaps.Count;
                lightmaps.Add(lightmapInfos[other].lightmap);
            }

            otherLightmapIndices[other] = lightmapIndex;
        }

        // Make lightmap texture readable
        for (int lightmap = 0; lightmap < lightmaps.Count; ++lightmap)
        {
            TextureImporter importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(lightmaps[lightmap])) as TextureImporter;
            importer.isReadable = true;
            importer.SaveAndReimport();
        }

        // Create Texture2DArray from lightmaps
        Texture2DArray batchedLightmapTex = new Texture2DArray(lightmaps[0].width, lightmaps[1].height, lightmaps.Count, TextureFormat.DXT1, false);
        for (int lightmap = 0; lightmap < lightmaps.Count; ++lightmap)
        {
            Texture2D tempTexture = new Texture2D(lightmaps[0].width, lightmaps[1].height, TextureFormat.RGB24, false, true);
            Color[] pixels = lightmaps[lightmap].GetPixels();
            tempTexture.SetPixels(pixels);
            tempTexture.Apply();
            EditorUtility.CompressTexture(tempTexture, TextureFormat.DXT1, TextureCompressionQuality.Best);
            Graphics.CopyTexture(tempTexture, 0, batchedLightmapTex, lightmap);
        }
        batchedLightmapTex.Apply();
        Scene scene = SceneManager.GetActiveScene();
        string outputPath = Path.Combine(Path.GetDirectoryName(scene.path), scene.name);
        AssetDatabase.CreateAsset(batchedLightmapTex, Path.Combine(outputPath, scene.name + "_lightmap.asset"));
        AssetDatabase.SaveAssets();

        SerializedProperty batchedLightmapProperty = serializedObject.FindProperty("batchedLightmap");
        batchedLightmapProperty.objectReferenceValue = batchedLightmapTex;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();

        // Update meshes
        Directory.Delete(Path.Combine(outputPath, "meshes"), true);
        Directory.CreateDirectory(Path.Combine(outputPath, "meshes"));

        for (int manager = 0; manager < lodManagers.Length; ++manager)
        {
            Vector4 lightmapScaleOffset = lodManagers[manager].lightmapParameters.lightmapScaleOffset;
            Vector2 lightmapScale = new Vector2(lightmapScaleOffset.x, lightmapScaleOffset.y);
            Vector2 lightmapOffset = new Vector2(lightmapScaleOffset.z, lightmapScaleOffset.w);
            for(int n = 0; n < lodManagers[manager].lodNodes.Count; ++n)
            {
                LODTreeNode node = lodManagers[manager].lodNodes[n];
                if (node.mesh != null)
                {
                    MeshFilter[] mfs = node.mesh.GetComponentsInChildren<MeshFilter>();
                    foreach (MeshFilter mf in mfs)
                    {
                        Mesh m = mf.sharedMesh;
                        string meshPath = AssetDatabase.GetAssetPath(m);
                        string newMeshPath = Path.Combine(outputPath, "meshes", Directory.GetParent(meshPath).Name + "_" + m.name + "_batched_" + lodManagerLightmapIndices[manager] + ".asset");
                        if (!File.Exists(newMeshPath))
                        {
                            m = Instantiate(m);
                            Vector2[] lightmapUVs = m.uv2;
                            Vector3[] newLightmapUVs = new Vector3[lightmapUVs.Length];
                            for (int i = 0; i < lightmapUVs.Length; ++i)
                            {
                                newLightmapUVs[i] = lightmapUVs[i] * lightmapScale + lightmapOffset;
                                newLightmapUVs[i].z = lodManagerLightmapIndices[manager];
                            }
                            m.SetUVs(1, newLightmapUVs);
                            AssetDatabase.CreateAsset(m, newMeshPath);
                            AssetDatabase.SaveAssets();
                        }
                        mf.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(newMeshPath);
                    }
                }
            }
        }

        for (int other = 0; other < lightmapInfos.Length; ++other)
        {
            Vector4 lightmapScaleOffset = lightmapInfos[other].lightmapScaleOffset;
            Vector2 lightmapScale = new Vector2(lightmapScaleOffset.x, lightmapScaleOffset.y);
            Vector2 lightmapOffset = new Vector2(lightmapScaleOffset.z, lightmapScaleOffset.w);

            MeshFilter[] mfs = lightmapInfos[other].GetComponentsInChildren<MeshFilter>();
            foreach (MeshFilter mf in mfs)
            {
                Mesh m = mf.sharedMesh;
                string meshPath = AssetDatabase.GetAssetPath(m);
                string newMeshPath = Path.Combine(outputPath, "meshes", Path.GetFileNameWithoutExtension(meshPath) + "_" + m.name + "_batched_" + otherLightmapIndices[other] + ".asset");
                if(!File.Exists(newMeshPath))
                {
                    m = Instantiate(m);

                    Vector2[] lightmapUVs = m.uv2;
                    if (lightmapUVs.Length == 0)
                        lightmapUVs = m.uv;
                    Vector3[] newLightmapUVs = new Vector3[lightmapUVs.Length];
                    for (int i = 0; i < lightmapUVs.Length; ++i)
                    {
                        newLightmapUVs[i] = lightmapUVs[i] * lightmapScale + lightmapOffset;
                        newLightmapUVs[i].z = otherLightmapIndices[other];
                    }
                    m.SetUVs(1, newLightmapUVs);
                    AssetDatabase.CreateAsset(m, newMeshPath);
                    AssetDatabase.SaveAssets();
                }
                mf.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(newMeshPath);
            }
        }

        // Enable shader keyword
        for (int manager = 0; manager < lodManagers.Length; ++manager)
        {
            MeshRenderer[] renderers = lodManagers[manager].GetComponentsInChildren<MeshRenderer>(true);
            foreach (MeshRenderer mr in renderers)
            {
                mr.sharedMaterial.EnableKeyword("BATCHED_LIGHTMAP");
            }
        }

        // Remove references to old lightmaps
        for (int i = 0; i < lodManagers.Length; ++i)
        {
            lodManagers[i].lightmapParameters = null;
            EditorUtility.SetDirty(lodManagers[i]);
        }

        for(int i = 0; i < lightmapInfos.Length; ++i)
        {
            DestroyImmediate(lightmapInfos[i]);
        }

        // Create scenes from LOD meshes
        Directory.Delete(Path.Combine(outputPath, "scenes"), true);
        Directory.CreateDirectory(Path.Combine(outputPath, "scenes"));
        List<EditorBuildSettingsScene> editorBuildSettingsScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        editorBuildSettingsScenes.RemoveAll(x => !File.Exists(x.path));

        for (int manager = 0; manager < lodManagers.Length; ++manager)
        {
            for (int n = 0; n < lodManagers[manager].lodNodes.Count; ++n)
            {
                LODTreeNode node = lodManagers[manager].lodNodes[n];
                if (node.mesh != null)
                {
                    Scene activeScene = EditorSceneManager.GetActiveScene();
                    Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                    EditorSceneManager.SetActiveScene(newScene);
                    GameObject sceneRoot = Instantiate(node.mesh);
                    sceneRoot.transform.position = node.mesh.transform.position;
                    sceneRoot.transform.rotation = node.mesh.transform.rotation;
                    sceneRoot.transform.localScale = node.mesh.transform.localScale;
                    string lodOutputPath = Path.Combine(outputPath, "scenes", "LOD" + node.lodLevel);
                    if (!Directory.Exists(lodOutputPath))
                        Directory.CreateDirectory(lodOutputPath);
                    string sceneName = Path.Combine(lodOutputPath, "M" + manager + "_LOD" + node.lodLevel + "_" + node.mesh.name + ".unity");
                    EditorSceneManager.SaveScene(newScene, sceneName);
                    EditorSceneManager.SetActiveScene(activeScene);
                    node.sceneIndex = editorBuildSettingsScenes.Count;

                    editorBuildSettingsScenes.Add(new EditorBuildSettingsScene(newScene.path, true));

                    if (PrefabUtility.IsPartOfPrefabInstance(node.mesh))
                    {
                        GameObject prefab = PrefabUtility.GetNearestPrefabInstanceRoot(node.mesh);
                        PrefabUtility.UnpackPrefabInstance(prefab, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    }
                    DestroyImmediate(node.mesh);
                    node.mesh = null;
                }
            }
        }

        EditorBuildSettings.scenes = editorBuildSettingsScenes.ToArray();
        EditorUtility.SetDirty(sublevelCombiner);
    }
}
