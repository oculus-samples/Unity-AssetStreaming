![AssetStreaming Banner](./Media/banner.png "AssetStreaming")

# AssetStreaming

AssetStreaming was built to demonstrate how to handle world loading without loading everything into memory at once. For more details and explanations you can visit our [Guide to Asset Streaming in Open World Game](https://developer.oculus.com/blog/now-available-guide-to-asset-streaming-in-open-world-games-/). More documentation that explains the ideas and the approach behind this project can be found [here](https://developer.oculus.com/documentation/unity/po-assetstreaming/).

This codebase is availabale as a reference to help you setup your own asset streaming project. Unity-AssetStreaming is under the license found [here](LICENSE) unless otherwise specified.

See the [CONTRIBUTING](CONTRIBUTING.md) file for how to help out.

## Licenses
The [Oculus License](LICENSE) applies to the SDK and supporting material.

The *[Demo Assets](./Assets/DemoAssets/)* used in this project are released under the *[Oculus Examples License](./Assets/DemoAssets/LICENSE.txt)*.

The MIT License applies to only certain, clearly marked documents. If an individual file does not indicate which license it is subject to, then the Oculus License applies.

## Getting started

First, ensure you have Git LFS installed by running this command:
```sh
git lfs install
```

Then, clone this repo using the "Code" button above, or this command:
```sh
git clone https://github.com/oculus-samples/Unity-AssetStreaming.git
```
To run the sample, open the project folder in *Unity 2021.3.26f1* or newer. Load the [Assets/Scenes/Startup](Assets/Scenes/Startup.unity) scene.

### Build
In order to build to device we need 2 steps. First we need to build the addressables assets, then we can build the apk. 

You can use the traditional route to build addressables from the groups menu (Window->Asset Management->Addressables->Groups). Then open Build Settings and click Build.

We also added an utility menu. Under AssetStreaming menu on the menu bar, you can do different builds:
* Build Addressables and Apk: build the addressables and the apk
* Build Addressables: Build's only the addressables
* Build Apk: Build's only the apk (usefull when only changing code)

## Oculus Integration Package
In order to keep the project simple, we kept only the required features from [Oculus Integration](https://assetstore.unity.com/packages/tools/integration/oculus-integration-82022). We kept [VR](Assets/Oculus/VR) and [Platform](Assets/Oculus/Platform). To update it, import the updated Asset Store package, and select only VR and Platform.

## Third-Party Libraries
In our implementation of the LOD Generator, we used a package from the Asset Store called [Mesh Baker](https://assetstore.unity.com/packages/tools/modeling/mesh-baker-5017). We highly recommend using Mesh Baker to create texture atlases and combine your meshes. Alternatively, you could write your own or get a license for something like Simplygon.

For Licenses reasons we didn't include [Mesh Baker](https://assetstore.unity.com/packages/tools/modeling/mesh-baker-5017) in the project.

# Conversion to use Addressables
We detailed how we converted the original Asset Streaming project to use Unity Addressables system [here](./ConversionToAddressables.md).

# AppLab
You can find the build version on AppLab:

https://www.oculus.com/experiences/quest/7325963400811201/
