# Convert AssetStreaming to use addressables


## Overview

This document will go over the process we went through to convert this project to use addressables. Addressables can help with managing assets, memory and iteration time. Here is some information on how it can help with [iteration time](https://developer.oculus.com/documentation/unity/po-unity-iteration/).

More information about Unity Addressables can be found in their [documentation](https://docs.unity3d.com/Packages/com.unity.addressables@1.18/manual/index.html).


## Initial Non Addressables Setup

The way the project works is that each LOD (level of details) chunk is a scene which is added in the Build Settings of the project and they each have a scene index. The first scene (0) being the one Unity will load at startup, the others can be referenced to be loaded when needed.

![image1](./Media/ConversionToAddressables/image1.png)


In the main scene (named combined, at index 0) we have multiple gameObjects with the LODManager component. In these components we have a list of nodes (LODTreeNode), they contain the data of what to load including the scene index from the Build Settings.

![image2](./Media/ConversionToAddressables/image2.png)

The LODManager will use that information to load the scene asynchronously in the hierarchy using SceneManager.LoadSceneAsync.


```
SceneManager.LoadSceneAsync(ao.sceneIndex, LoadSceneMode.Additive);
```



## Updating to Addressables


### How to use Addressables

There are different ways to reference an addressable asset either using the full address of the asset as specified in the addressable manifest (we will see what that address is later) or use the AssetReference data which will contain the information needed to load the asset from the addressable bundle.

For our case the AssetReference is great, this way the asset address can change but our reference will always know what asset to use. Using the full address method is great for dynamic loading like getting data from cms, from a file or any text data.

Now that we know what we want to use, let's see what we need to do. We want to replace the scene index by an AssetReference, but first we need to convert our scenes to use the addressable system.


### Add the addressable package

Go to Window->Package Manager

![image3](./Media/ConversionToAddressables/image3.png)


Install the Addressables package from the Unity Registry.

Now that it's added to our project we can set it up, open the Addressables Group window (Window->Asset Management->Addressables->Groups)

Click Create Addressables Settings

![image4](./Media/ConversionToAddressables/image4.png)

This will create basic settings. You will now see 2 groups:



* Built in data: This is all the data referenced when we build the client (Scenes and Resources)
* Default Local Group: This is a new addressable group for local assets. It is empty to start and that's where we will add our Scenes.
    * Local assets means that they will be packaged with the build. Remote asset means it's hosted somewhere like an asset server (we will not go in remote asset details in this note)

Now we are ready to convert our scenes to be included in the addressable group.


### Add Scenes in addressables group and reference them

We are in a situation where we have a lot of nodes in the different LODManager from the scene and it will be easier to automate the conversion instead of doing it manually. We already know that we want to add an AssetReference in the LODTreeNode so we can reference the scene. Let's start by adding that field in the LODTreeNode right after the sceneIndex.


```
public int sceneIndex = -1;
public AssetReference sceneRef = null; // New field added
```


Now that we have this field we can go over every node and use the sceneIndex set to add that scene to the Default addressable group and link the scene in the sceneRef field.

This snippet of code will process a LODTreeNode, get the sceneIndex so we can get the path of the scene from the BuildSettings. Then create an addressable entry in the settings and assign that scene to the DefaultGroup. Set the address of the addressable entry to the GUID of the scene. Finally, we assign a new AssetReference using the GUID to the node sceneRef field.


```
var settings = AddressableAssetSettingsDefaultObject.Settings;
var group = settings.DefaultGroup;
if (node.sceneIndex != -1)
{
   string scenePath = SceneUtility.GetScenePathByBuildIndex(node.sceneIndex);
#if  UNITY_2019
   string guid = AssetDatabase.AssetPathToGUID(scenePath);
#else
   GUID guid = AssetDatabase.GUIDFromAssetPath(scenePath);
#endif
   var entry = settings.CreateOrMoveEntry(guid.ToString(), group, readOnly: false, postEvent: true);
   entry.address = AssetDatabase.GUIDToAssetPath(guid);
   node.sceneRef = new AssetReference(guid.ToString());
}
```


Here is what the node data looks like with the reference

![image5](./Media/ConversionToAddressables/image5.png)


And the scene is now set as addressable with a specific address which is the scene GUID. That address could be used to load the asset from the address as mentioned earlier instead of using the AssetReference.

![image6](./Media/ConversionToAddressables/image6.png)

When a scene is marked as addressable it generously does the work of disabling it from the Build Settings for us. So our build settings should now look like this:

![image7](./Media/ConversionToAddressables/image7.png)


All runtime scenes are disabled since we don't want them built with the executable.

Important: Save the Scene

The last thing is to save the main scene (you might have to do a small change to it so that it's marked dirty or set it dirty from your update script) so that we save the changes to the LODManagers.


#### Separate Scenes in relevant groups

In order to make faster iteration when modifying a certain location, we separated the assets in groups based on location so that only the modified group would need to be rebuilt. We also created a group for shared assets so that some reused assets are not part of each group.

![image8](./Media/ConversionToAddressables/image8.png)

### Load Scenes from Addressable

Now that we have the scenes in an addressable group we need to update the code to load from addressable.

We need to change the call we saw earlier


```
SceneManager.LoadSceneAsync(ao.sceneIndex, LoadSceneMode.Additive);
```


To use the sceneRef field and load the scene using that AssetReference. They have a method called LoadSceneAsync where we can set the scene load mode and the priority of the async thread.


```
ao.sceneRef.LoadSceneAsync(LoadSceneMode.Additive, priority:ao.lodLevel);
```


This method will replace our previous call, but we have a little more to do around it since it returns an handle on the operation, AsyncOperationHandle, therefore we need to update how we handle this data. It's not much more different than having a reference to UnityEngine.AsyncOperation, but instead of the actual operation we keep the handle. For the scenes it returns an handle on the operation that loads a SceneInstance, AsyncOperationHandle&lt;SceneInstance>. We then need to update the fields that were keeping the AsyncOperation for the Handle and update our callback like onSceneLoadComplete to take in the handle as a parameter.

Finally for unloading the scene we used that saved handle and call the addressable unload scene method, which will give us a different handle that we can use to observe the status of the operation and add callbacks when completed.


```
Addressables.UnloadSceneAsync(ao.sceneHandle);
