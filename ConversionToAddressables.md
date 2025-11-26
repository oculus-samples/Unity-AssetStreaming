# Convert AssetStreaming to Use Addressables

## Overview

This document outlines the process of converting this project to use addressables. Addressables help manage assets, memory, and iteration time. Learn more about how they improve [iteration time](https://developers.meta.com/horizon/documentation/unity/po-unity-iteration/).

For more information on Unity Addressables, refer to their [documentation](https://docs.unity3d.com/Packages/com.unity.addressables@1.18/manual/index.html).

## Initial Non-Addressables Setup

In the project, each LOD (level of detail) chunk is a scene added in the Build Settings, each with a scene index. The first scene (0) loads at startup, while others load as needed.

![image1](./Media/ConversionToAddressables/image1.png)

In the main scene (named "combined," at index 0), multiple gameObjects have the LODManager component. These components contain a list of nodes (LODTreeNode) with data on what to load, including the scene index from the Build Settings.

![image2](./Media/ConversionToAddressables/image2.png)

The LODManager uses this information to load scenes asynchronously using `SceneManager.LoadSceneAsync`.

```csharp
SceneManager.LoadSceneAsync(ao.sceneIndex, LoadSceneMode.Additive);
```

## Updating to Addressables

### How to Use Addressables

Addressable assets can be referenced using the full address from the addressable manifest or the AssetReference data, which contains the necessary information to load the asset from the addressable bundle.

For our case, AssetReference is ideal. It allows the asset address to change while maintaining the correct reference. The full address method is useful for dynamic loading, such as retrieving data from a CMS, file, or text data.

To proceed, we need to replace the scene index with an AssetReference and convert our scenes to use the addressable system.

### Add the Addressable Package

Navigate to Window -> Package Manager.

![image3](./Media/ConversionToAddressables/image3.png)

Install the Addressables package from the Unity Registry. Once added, set it up by opening the Addressables Group window (Window -> Asset Management -> Addressables -> Groups) and clicking "Create Addressables Settings."

![image4](./Media/ConversionToAddressables/image4.png)

This creates basic settings, resulting in two groups:

- **Built-in Data**: Data referenced when building the client (Scenes and Resources).
- **Default Local Group**: A new addressable group for local assets, initially empty. This is where we will add our scenes. Local assets are packaged with the build, while remote assets are hosted elsewhere (details on remote assets are not covered here).

Now, we are ready to convert our scenes to be included in the addressable group.

### Add Scenes to Addressables Group and Reference Them

We have many nodes in different LODManagers, so automating the conversion is easier than doing it manually. We will add an AssetReference in the LODTreeNode to reference the scene. Add this field in the LODTreeNode right after the sceneIndex.

```csharp
public int sceneIndex = -1;
public AssetReference sceneRef = null; // New field added
```

With this field, we can process each node, use the sceneIndex to add the scene to the Default addressable group, and link it in the sceneRef field.

This code snippet processes a LODTreeNode, retrieves the sceneIndex to get the scene path from the BuildSettings, creates an addressable entry, assigns it to the DefaultGroup, sets the address to the scene's GUID, and assigns a new AssetReference using the GUID to the node's sceneRef field.

```csharp
var settings = AddressableAssetSettingsDefaultObject.Settings;
var group = settings.DefaultGroup;
if (node.sceneIndex != -1)
{
   string scenePath = SceneUtility.GetScenePathByBuildIndex(node.sceneIndex);
#if UNITY_2019
   string guid = AssetDatabase.AssetPathToGUID(scenePath);
#else
   GUID guid = AssetDatabase.GUIDFromAssetPath(scenePath);
#endif
   var entry = settings.CreateOrMoveEntry(guid.ToString(), group, readOnly: false, postEvent: true);
   entry.address = AssetDatabase.GUIDToAssetPath(guid);
   node.sceneRef = new AssetReference(guid.ToString());
}
```

The node data now includes the reference:

![image5](./Media/ConversionToAddressables/image5.png)

The scene is now addressable with a specific address, the scene GUID, which can be used to load the asset instead of using the AssetReference.

![image6](./Media/ConversionToAddressables/image6.png)

When a scene is marked as addressable, it is automatically disabled in the Build Settings. The build settings should now look like this:

![image7](./Media/ConversionToAddressables/image7.png)

All runtime scenes are disabled to prevent them from being built with the executable.

**Important**: Save the Scene

Save the main scene (you might need to make a small change to mark it dirty or set it dirty from your update script) to save changes to the LODManagers.

#### Separate Scenes into Relevant Groups

To speed up iteration when modifying a location, separate assets into groups based on location so only the modified group needs rebuilding. Create a group for shared assets to avoid including reused assets in each group.

![image8](./Media/ConversionToAddressables/image8.png)

### Load Scenes from Addressable

With scenes in an addressable group, update the code to load from addressable.

Change the previous call:

```csharp
SceneManager.LoadSceneAsync(ao.sceneIndex, LoadSceneMode.Additive);
```

To use the sceneRef field and load the scene using AssetReference. Use the LoadSceneAsync method to set the scene load mode and async thread priority.

```csharp
ao.sceneRef.LoadSceneAsync(LoadSceneMode.Additive, priority: ao.lodLevel);
```

This method replaces the previous call. Since it returns an AsyncOperationHandle, update how this data is handled. It's similar to having a reference to UnityEngine.AsyncOperation, but instead of the actual operation, keep the handle. For scenes, it returns an AsyncOperationHandle<SceneInstance>. Update fields that kept the AsyncOperation for the Handle and update callbacks like onSceneLoadComplete to take the handle as a parameter.

For unloading the scene, use the saved handle and call the addressable unload scene method. This provides a different handle to observe the operation's status and add callbacks upon completion.

```csharp
Addressables.UnloadSceneAsync(ao.sceneHandle);
```
