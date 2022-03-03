// Copyright (c) Meta Platforms, Inc. and affiliates.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Serializes the lightmap information of meshes managed by the LOD manager.
// Used to combine the lightmaps sublevels into a texture array in SublevelCombinerEditor.cs
[System.Serializable]
public class LightmapParameters
{
    public Texture2D lightmap;
    public Vector4 lightmapScaleOffset;
}

// Manages the LOD system of a sublevel
public class LODManager : MonoBehaviour
{
    public Material debugMaterial;
    public bool debugLODLevels = false;
    public int forceLOD = -1;
    public bool freezeLOD = false;
    public Transform testPosition;
    public Vector2 gridCellSize;
    private Vector2 lastUpdatePosition;
    public LightmapParameters lightmapParameters;
    public List<LODTreeNode> lodNodes;
    private List<Mesh> assetsToUnload;

    public Vector2Int cameraCell;
    [HideInInspector]
    public Material[] lodDebugMaterials;

    // Stores basic information about asynchrounes load or unload operations so the scheduling of these operations can be spread over multiple frames
    public class AsyncOperation
    {
        public int sceneIndex;
        public int lodLevel;
        public Action<UnityEngine.AsyncOperation> onSceneLoadComplete;
        public Scene scene;
        public Action<UnityEngine.AsyncOperation> onSceneUnloadComplete;
        public Action<UnityEngine.AsyncOperation> setAsyncOperation;

        // Constructor for load operations
        public AsyncOperation(int sceneIndex, int lodLevel, Action<UnityEngine.AsyncOperation> onSceneLoadComplete, Action<UnityEngine.AsyncOperation> setAsyncOperation)
        {
            this.sceneIndex = sceneIndex;
            this.lodLevel = lodLevel;
            this.onSceneLoadComplete = onSceneLoadComplete;
            this.setAsyncOperation = setAsyncOperation;
        }

        // Constructor for unload operations
        public AsyncOperation(Scene scene, Action<UnityEngine.AsyncOperation> onSceneUnloadComplete, Action<UnityEngine.AsyncOperation> setAsyncOperation)
        {
            this.scene = scene;
            this.onSceneUnloadComplete = onSceneUnloadComplete;
            this.setAsyncOperation = setAsyncOperation;
        }
    }

    private Queue<AsyncOperation> asyncOperations; 

    // Initialize the LOD system
    private void Start()
    {
        // Create an instance of the lod debug material for each of the LOD levels +1 for mesh which are loading
        if(debugMaterial != null)
        {
            lodDebugMaterials = new Material[4];
            for(int i = 0; i < lodDebugMaterials.Length; ++i)
            {
                lodDebugMaterials[i] = new Material(debugMaterial);
                lodDebugMaterials[i].SetFloat("_LODState", i);
                if(lightmapParameters.lightmap == null)
                    lodDebugMaterials[i].EnableKeyword("BATCHED_LIGHTMAP");
            }
        }

        cameraCell = new Vector2Int(int.MaxValue, int.MaxValue);
        lastUpdatePosition = new Vector2(float.MaxValue, float.MaxValue);
        assetsToUnload = new List<Mesh>();
        asyncOperations = new Queue<AsyncOperation>();
        
        if(lodNodes == null)
        {
            Debug.LogError("No lod nodes. LOD system will not work.");
        }
        else
        {
            // Initialize the LOD system
            lodNodes[lodNodes.Count - 1].Initialize(this, -1);
            lodNodes[lodNodes.Count - 1].DisableAll(this);
        }
    }

    // Update the LOD system and execture queued load/unload operation and asset unload operations
    void Update()
    {
        UpdateLODs(false);

        // Start up to 4 asynchronous load/unload operations per frame.
        // Done to spread out the sheduling of these operations over multiple frames.
        // Sheduling the operations can be expensive on the main thread.
        for(int i = 0; i < 4 && asyncOperations.Count != 0; ++i)
        {
            UnityEngine.Profiling.Profiler.BeginSample("Execute Async Operations");
            AsyncOperation ao = asyncOperations.Dequeue();
            if(ao.onSceneLoadComplete != null)
            {
                UnityEngine.AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(ao.sceneIndex, LoadSceneMode.Additive);
                asyncOperation.priority = ao.lodLevel;
                asyncOperation.completed += ao.onSceneLoadComplete;
                ao.setAsyncOperation(asyncOperation);
            }
            else
            {
                UnityEngine.AsyncOperation asyncOperation = SceneManager.UnloadSceneAsync(ao.scene, UnloadSceneOptions.UnloadAllEmbeddedSceneObjects);
                asyncOperation.completed += ao.onSceneUnloadComplete;
                ao.setAsyncOperation(asyncOperation);
            }
            UnityEngine.Profiling.Profiler.EndSample();
        }

        // Unload assets no longer in use.
        // Is an expensive call. Spread calls over multiple frames to avoid hitches.
        if (assetsToUnload.Count != 0)
        {
            UnityEngine.Profiling.Profiler.BeginSample("Unload Assets");
            Mesh assetToUnload = assetsToUnload[assetsToUnload.Count - 1];
            assetsToUnload.RemoveAt(assetsToUnload.Count - 1);
            Resources.UnloadAsset(assetToUnload);
            UnityEngine.Profiling.Profiler.EndSample();
        }
    }

    // Toggle the debug shader on all meshes in the LOD system
    public void SetLODDebugView(bool enabled)
    {
        debugLODLevels = enabled;
        UpdateLODs(true);
    }

    // Force the lod level of all meshes in the LOD system
    public void ForceLOD(int lod)
    {
        forceLOD = lod;
        UpdateLODs(true);
    }

    // Update the visible meshes in the LOD system if necessary
    public void UpdateLODs(bool forceLODUpdate)
    {
        if(forceLODUpdate)
            lodNodes[lodNodes.Count - 1].Reset();

        Vector2 treeSize = gridCellSize * Mathf.Pow(2, lodNodes[lodNodes.Count - 2].lodLevel);
        Camera camera = Camera.main;
        Vector3 cameraPos = camera.transform.position;
        if (testPosition != null)
            cameraPos = testPosition.position; // Override for camera position. Useful for debugging in the editor.
        cameraPos -= transform.position;

        Vector2 cameraPos2D = new Vector2(cameraPos.x, cameraPos.z);

        // Stop the LOD system from updating if the camera hasn't moved enough since the last time it was updated
        // or if the LOD system is frozen.
        // (cameraPos2D - lastUpdatePosition) check stops the LODs from changing rapidly when the player is on the edge between grid cells.
        if (!forceLODUpdate && ((cameraPos2D - lastUpdatePosition).sqrMagnitude < 1.0f || freezeLOD))
            return;

        // The LOD system is based on a grid. So we only have to update the LODs when the camera has entered a new cell.
        Vector2Int currentCameraCell = Vector2Int.zero;
        currentCameraCell.x = Mathf.FloorToInt(cameraPos.x / gridCellSize.x);
        currentCameraCell.y = Mathf.FloorToInt(cameraPos.z / gridCellSize.y);

        if (lodNodes != null && (cameraCell != currentCameraCell || forceLODUpdate))
        {
            lastUpdatePosition = cameraPos2D;
            Vector2 positionRelativeToNode = new Vector2(cameraPos.x / treeSize.x + 1.0f, cameraPos.z / treeSize.y + 1.0f) * 0.5f;
            lodNodes[lodNodes.Count - 1].UpdateVisibility(positionRelativeToNode);
            lodNodes[lodNodes.Count - 1].ApplyVisibility();
            cameraCell = currentCameraCell;
        }
    }

    // Populate the LOD system. Only used by LODGenerator.cs
    // This creates an quadtree from the provided lods.
    public void SetLOD(List<List<LODTreeNode>> lods)
    {
        // Create the tree
        lodNodes = new List<LODTreeNode>();
        for (int lod = 0; lod < lods.Count; ++lod)
        {
            if (lods[lod].Count <= 4)
                break;

            for (int mesh = 0; mesh < lods[lod].Count; ++mesh)
            {
                Vector2Int cell = lods[lod][mesh].cell;
                Vector2Int parentCell = cell;
                parentCell.x = Mathf.FloorToInt(parentCell.x / 2.0f);
                parentCell.y = Mathf.FloorToInt(parentCell.y / 2.0f);

                int lodIndex = lodNodes.Count;
                lodNodes.Add(lods[lod][mesh]);
                lodNodes[lodIndex].lodLevel = lod;
                lodNodes[lodIndex].nodeIndex = lodIndex;

                int parentIndex = lod < lods.Count - 1 ? lods[lod + 1].FindIndex(x => x.cell == parentCell) : -1;
                LODTreeNode parent = null;
                if (parentIndex < 0)
                {
                    parent = new LODTreeNode();
                    parent.cell = parentCell;
                    if (lods.Count > lod + 1)
                        lods[lod + 1].Add(parent);
                    else
                        lods.Add(new List<LODTreeNode>(new LODTreeNode[] { parent }));
                }
                else
                    parent = lods[lod + 1][parentIndex];

                if(parent.children == null)
                    parent.children = new int[4] { -1, -1, -1, -1 };
                
                cell.x = Mathf.Abs(cell.x);
                cell.y = Mathf.Abs(cell.y);

                // The child index is always in order and based on the position
                // children[0] = cell 0, 0
                // children[1] = cell 1, 0
                // children[2] = cell 0, 1
                // children[3] = cell 1, 1
                // This is important when calculating the LOD level. See LODTreeNode::UpdateVisibility.
                int childIndex = ((cell.x % 2) * 2 + (cell.y % 2));
                parent.children[childIndex] = lodIndex;
            }
        }

        // Fix last nodes
        for(int mesh = 0; mesh < lods[lods.Count - 1].Count; ++mesh)
        {
            int lodIndex = lodNodes.Count;
            lodNodes.Add(lods[lods.Count - 1][mesh]);
            lodNodes[lodIndex].lodLevel = lods.Count - 1;
            lodNodes[lodIndex].nodeIndex = lodIndex;
        }

        // Create root node
        LODTreeNode rootNode = new LODTreeNode();
        rootNode.lodLevel = lods.Count;
        rootNode.nodeIndex = lodNodes.Count;
        rootNode.children = new int[4] { -1, -1, -1, -1 };

        int negativeX = 0, negativeY = 0;
        for (int mesh = 0; mesh < lods[lods.Count - 1].Count; ++mesh)
        {
            Vector2Int cell = lods[lods.Count - 1][mesh].cell;
            if (cell.x < 0) negativeX = 1;
            if (cell.y < 0) negativeY = 1;
        }

        for (int mesh = 0; mesh < lods[lods.Count - 1].Count; ++mesh)
        {
            Vector2Int cell = lods[lods.Count - 1][mesh].cell;
            cell.x = Mathf.Abs(cell.x + negativeX);
            cell.y = Mathf.Abs(cell.y + negativeY);
            int childIndex = ((cell.x % 2) * 2 + (cell.y % 2));
            rootNode.children[childIndex] = lods[lods.Count - 1][mesh].nodeIndex;
        }

        lodNodes.Add(rootNode);
    }

    // Queues an async operation. Load or unload.
    public void QueueAsyncOperation(AsyncOperation asyncOperation)
    {
        asyncOperations.Enqueue(asyncOperation);
    }

    // Get a node based on the node index.
    public LODTreeNode GetNode(int i)
    {
        return lodNodes[i];
    }

    // Queue asset unload operation
    public void QueueAssetUnload(Mesh mesh)
    {
        assetsToUnload.Add(mesh);
    }

    // Remove asset unload operation from queue
    public void DequeueAssetUnload(Mesh mesh)
    {
        assetsToUnload.Remove(mesh);
    }
}
