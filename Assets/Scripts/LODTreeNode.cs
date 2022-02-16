// Copyright (c) Meta Platforms, Inc. and affiliates.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

// Node used in the LOD system.
// Nodes contain all logic for switching and streaming LOD meshes.

[System.Serializable]
public class LODTreeNode
{
    public enum NodeState
    {
        Unloaded,   // Not visible & not in memory
        Loaded,     // Not visible but in memory
        Enabled     // Visible & in memory
    }

    // Stores info needed to queue asynchrounes Load or Unload operations
    public class LoadOperation
    {
        public bool processing = false;
        public bool load; // Is load or unload operation
        public AsyncOperationHandle<SceneInstance> asyncOperationHandle; // Store asynchrounes operation handles
    }

    public LODManager lodManager = null;
    public Vector2Int cell;
    public int lodLevel = -1;
    public int nodeIndex = -1;
    public GameObject mesh;
    public AssetReference sceneRef = null;
    public NodeState nodeState;
    public Scene scene;
    public AsyncOperationHandle<SceneInstance> sceneHandle;
    private List<Mesh> meshAssets;
    public bool sceneIsShown = false;
    public int[] children = null;
    public LoadOperation[] loadOperations = new LoadOperation[2];
    public int parentNodeIndex = -1;
    public int childNodesLoading = 0;
    private List<GameObject> sceneRootObjects;
    private bool asyncUnloadQueued = false;

    public void Initialize(LODManager lodManager, int parentNodeIndex)
    {
        this.lodManager = lodManager;
        this.parentNodeIndex = parentNodeIndex;
        sceneRootObjects = new List<GameObject>();

        for (int i = 0; i < children.Length; ++i)
        {
            if (children[i] != -1)
            {
                lodManager.GetNode(children[i]).Initialize(lodManager, nodeIndex);
                ++childNodesLoading;
            }
        }
    }

    // Unloads this node & all child nodes
    public void Reset()
    {
        nodeState = NodeState.Unloaded;
        if (sceneRef.RuntimeKeyIsValid())
        {
            HideScene();
            if (scene.isLoaded && !asyncUnloadQueued)
            {
                Unload();
            }
        }

        for (int i = 0; i < children.Length; ++i)
        {
            if (children[i] != -1)
            {
                lodManager.GetNode(children[i]).Reset();
            }
        }
    }

    // Calculate the state of the LOD node. To be applied later.
    public void UpdateVisibility(Vector2 positionRelativeToNode)
    {
        bool lodLevelForced = lodManager.forceLOD != -1 && lodLevel <= 2;

        // Calculate if the camera is close to the current node. The camera is considered close if it's less than 1 node width away from the current node.
        // positionRelativeToNode scaled by the size of the node. Ex. If the node is 40 units wide then positionRelativeToNode(1, -1) means that the camera is (20, -20) units away from the center of this node in worldspace.
        bool closeToCamera = positionRelativeToNode.x > -1.0f && positionRelativeToNode.x < 2.0f && positionRelativeToNode.y > -1.0f && positionRelativeToNode.y < 2.0f && !lodLevelForced;
        
        if((closeToCamera || (lodLevelForced && lodManager.forceLOD != lodLevel)) && children.Length > 0)
        {
            for (int i = 0; i < 4; ++i)
            {
                // Child nodes are always in the same order depending on their relative position within the current node.
                // children[0] = 0, 0
                // children[1] = 1, 0
                // children[2] = 0, 1
                // children[3] = 1, 1
                // positionRelativeToNode of the child node needs to be offset depending on their position withing the current node.
                Vector2 childRelativePosition = positionRelativeToNode * 2.0f;
                childRelativePosition.x -= (i / 2);
                childRelativePosition.y -= (i % 2);

                if (children[i] != -1)
                {
                    lodManager.GetNode(children[i]).UpdateVisibility(childRelativePosition);
                }
            }

            // The child nodes are visible. This node has to be kept in memory so we can switch to it when the child nodes go out of range or are still loading.
            nodeState = NodeState.Loaded;
        }
        else
        {
            nodeState = NodeState.Enabled;
            // Disable the child meshes in case they're still active.
            // The child nodes of a node that's enabled can never also be enabled.
            DisableChildren(lodManager);
        }
    }

    // Makes the LOD visible if possible.
    public void Enable()
    {
        if (mesh != null)
        {
            mesh.SetActive(true);
        }

        if (!scene.isLoaded || asyncUnloadQueued)
        {
            Load();
        }
        else
        {
            ShowScene();
        }
    }

    // Execute the next load operation in the queue
    public void HandleLoadOperations()
    {
        if (loadOperations[0] != null)
        {
            if (loadOperations[0].processing)
            {
                if (loadOperations[0].asyncOperationHandle.IsDone)
                {
                    // Previous operation is done. Remove it from the queue.
                    loadOperations[0].processing = false;
                    loadOperations[0] = loadOperations[1];
                    loadOperations[1] = null;

                    HandleLoadOperations();
                }
            }
            else
            {
                if (loadOperations[0].load)
                {
                    lodManager.QueueAsyncOperation(new LODManager.AsyncOperation(sceneRef, lodLevel, OnSceneLoadComplete, SetAsyncOperation));
                }
                else
                {
                    lodManager.GetNode(parentNodeIndex).ChildUnloaded();
                    lodManager.QueueAsyncOperation(new LODManager.AsyncOperation(sceneHandle, OnSceneUnloadComplete, SetAsyncOperation));
                    asyncUnloadQueued = true;
                }
            }
        }
    }

    // Queue a asynchrounes load operation if neccessary
    public void Load()
    {
        if (!sceneRef.RuntimeKeyIsValid())
            return;

        if (loadOperations[1] != null && !loadOperations[1].load)
            loadOperations[1] = null; // Remove waiting unload operation from queue

        if (loadOperations[0] != null)
        {
            if (loadOperations[0].load)
                return; // Load already in progress
            else
            {
                loadOperations[1] = new LoadOperation() { load = true };
                return;
            }
        }

        loadOperations[0] = new LoadOperation() { load = true };
        HandleLoadOperations();
    }

    // Queue asynchrounes unload operation if necessary
    public void Unload()
    {
        if (!sceneRef.RuntimeKeyIsValid())
            return;

        if (loadOperations[1] != null && loadOperations[1].load)
            loadOperations[1] = null; // Remove waiting load operation from queue

        if (loadOperations[0] != null)
        {
            if (!loadOperations[0].load)
                return; // Unload already in progress
            else
            {
                loadOperations[1] = new LoadOperation() { load = false };
                return;
            }
        }

        loadOperations[0] = new LoadOperation() { load = false };
        HandleLoadOperations();
    }

    // Toggle mesh visibility depending on the node state
    public void ApplyVisibility()
    {
        if (sceneRef.RuntimeKeyIsValid())
        {
            if (nodeState == NodeState.Unloaded)
            {
                HideScene();
                if (scene.isLoaded && !asyncUnloadQueued)
                {
                    Unload();
                }
            }
            else if (nodeState == NodeState.Loaded)
            {
                if (!scene.isLoaded || asyncUnloadQueued)
                {
                    Load();
                }
                else
                {
                    HideScene();
                }
            }
            else if (nodeState == NodeState.Enabled)
            {
                Enable();
            }
        }
        else if(mesh != null)
        {
            // Meshes are only used in sublevels. Before the sublevels get combined into 1 big level.
            // Only used for debugging.
            if (nodeState == NodeState.Unloaded)
            {
                mesh.SetActive(false);
            }
            else if (nodeState == NodeState.Loaded)
            {
                mesh.SetActive(false);
            }
            else if (nodeState == NodeState.Enabled)
            {
                mesh.SetActive(true);
            }
        }
        
        for (int i = 0; i < children.Length; ++i)
        {
            if (children[i] != -1)
            {
                lodManager.GetNode(children[i]).ApplyVisibility();
            }
        }
        
        // If any of the child nodes are still loading we keep this mesh enabled until they've finished loading.
        if(childNodesLoading != 0 && nodeState == NodeState.Loaded)
        {
            Enable();
        }
    }

    // Assign the LOD debug material if necessary
    public void UpdateDebugMaterial()
    {
        if (lodManager.debugLODLevels && sceneIsShown)
        {
            foreach (GameObject go in sceneRootObjects)
            {
                MeshRenderer[] mrs = go.GetComponentsInChildren<MeshRenderer>();
                foreach (MeshRenderer mr in mrs)
                {
                    mr.sharedMaterial = lodManager.lodDebugMaterials[lodLevel];
                    if(nodeState == NodeState.Loaded)
                    {
                        mr.sharedMaterial = lodManager.lodDebugMaterials[3];
                    }
                }
            }
        }

    }

    // Disables this and all child nodes
    public void DisableAll(LODManager lodManager)
    {
        nodeState = NodeState.Unloaded;
        DisableChildren(lodManager);
    }

    // Disables on the child nodes
    public void DisableChildren(LODManager lodManager)
    {
        for (int i = 0; i < children.Length; ++i)
        {
            if (children[i] != -1)
            {
                lodManager.GetNode(children[i]).DisableAll(lodManager);
            }
        }
    }

    // Asynchronous load operation callback
    public void OnSceneLoadComplete(AsyncOperationHandle<SceneInstance> asyncOperationHandle)
    {
        sceneHandle = asyncOperationHandle;
        scene = sceneHandle.Result.Scene;
        sceneRootObjects.Clear();
        scene.GetRootGameObjects(sceneRootObjects);
        sceneIsShown = true;
        bool neighboursStillLoading = lodManager.GetNode(parentNodeIndex).ChildFinishedLoading();
        if (nodeState == NodeState.Loaded || neighboursStillLoading)
        {
            HideScene();
        }
        UpdateDebugMaterial();
        CollectMeshes();

        HandleLoadOperations();
    }

    // Asynchronous unload operation callback
    public void OnSceneUnloadComplete(AsyncOperationHandle<SceneInstance> asyncOperation)
    {
        UnloadMeshes();
        sceneIsShown = false;
        HandleLoadOperations();
    }

    // Is called by child mesh to let parent node know how many child nodes have finished loading
    public bool ChildFinishedLoading()
    {
        --childNodesLoading;
        Debug.Assert(childNodesLoading >= 0);

        if(childNodesLoading == 0)
        {
            bool childrenEnabled = false;
            for (int i = 0; i < children.Length; ++i)
            {
                if (children[i] != -1)
                {
                    LODTreeNode childNode = lodManager.GetNode(children[i]);
                    if (childNode.nodeState == NodeState.Enabled)
                    {
                        childNode.Enable();
                        childrenEnabled = true;
                    }
                }
            }

            if(childrenEnabled)
                HideScene();

            return !childrenEnabled;
        }

        return true;
    }

    // Is called by child mesh to let parent node know how many child nodes are not loaded
    public void ChildUnloaded()
    {
        ++childNodesLoading;
    }

    // Make LOD visible
    public void ShowScene()
    {
        if (sceneIsShown)
        {
            UpdateDebugMaterial();
            return;
        }

        foreach (GameObject go in sceneRootObjects)
            go.SetActive(true);

        sceneIsShown = true;
        UpdateDebugMaterial();
    }

    // Make LOD invisible
    public void HideScene()
    {
        if (!sceneIsShown)
            return;

        foreach (GameObject go in sceneRootObjects)
            go.SetActive(false);

        sceneIsShown = false;
    }

    // Get references to all meshes used by LOD. Used to unload unused assets.
    private void CollectMeshes()
    {
        meshAssets = new List<Mesh>();
        
        foreach (GameObject go in sceneRootObjects)
        {
            MeshFilter[] mfs = go.GetComponentsInChildren<MeshFilter>();
            foreach (MeshFilter mf in mfs)
            {
                meshAssets.Add(mf.sharedMesh);
                lodManager.DequeueAssetUnload(mf.sharedMesh);
            }
        }
    }

    // Queue meshes to be unloaded
    private void UnloadMeshes()
    {
        if (meshAssets != null)
        {
            for (int i = 0; i < meshAssets.Count; ++i)
            {
                lodManager.QueueAssetUnload(meshAssets[i]);
            }

            meshAssets = null;
        }
    }

    // Callback to retrieve asynchronous operation handle
    public void SetAsyncOperation(AsyncOperationHandle<SceneInstance> asyncOperationHandle)
    {
        loadOperations[0].processing = true;
        loadOperations[0].asyncOperationHandle = asyncOperationHandle;
        if (!loadOperations[0].load)
            asyncUnloadQueued = false;
    }
}
