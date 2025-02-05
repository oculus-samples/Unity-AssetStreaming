![AssetStreaming Banner](./Media/banner.png "AssetStreaming")

# AssetStreaming

AssetStreaming was built to demonstrate how to handle world loading without loading everything into memory at once. For more details and explanations you can visit our [Guide to Asset Streaming in Open World Game](https://developer.oculus.com/blog/now-available-guide-to-asset-streaming-in-open-world-games-/). More documentation that explains the ideas and the approach behind this project can be found [here](https://developer.oculus.com/documentation/unity/po-assetstreaming/).

This codebase is availabale as a reference to help you setup your own asset streaming project. Unity-AssetStreaming is under the license found [here](LICENSE) unless otherwise specified.

See the [CONTRIBUTING](CONTRIBUTING.md) file for how to help out.

## Licenses
The majority of Asset Streaming is licensed under [MIT LICENSE](LICENSE), however files listed below are licensed under their respective licensing terms.
* [TextMeshPro](./Assets/TextMesh Pro) - [Unity Companion License](http://www.unity3d.com/legal/licenses/Unity_Companion_License)
* [Demo Assets](./Assets/DemoAssets/) - [Meta Platforms Technologies Examples License](./Assets/DemoAssets/LICENSE.txt)

## Getting started

First, ensure you have Git LFS installed by running this command:
```sh
git lfs install
```

Then, clone this repo using the "Code" button above, or this command:
```sh
git clone https://github.com/oculus-samples/Unity-AssetStreaming.git
```
To run the sample, open the project folder in *Unity 2022.3.52f1* or newer. Load the [Assets/Scenes/Startup](Assets/Scenes/Startup.unity) scene.

### Build
In order to build to device we need 2 steps. First we need to build the addressables assets, then we can build the apk. 

You can use the traditional route to build addressables from the groups menu (Window->Asset Management->Addressables->Groups). Then open Build Settings and click Build.

We also added an utility menu. Under AssetStreaming menu on the menu bar, you can do different builds:
* Build Addressables and Apk: build the addressables and the apk
* Build Addressables: Build's only the addressables
* Build Apk: Build's only the apk (usefull when only changing code)

## Third-Party Libraries
In our implementation of the LOD Generator, we used a package from the Asset Store called [Mesh Baker](https://assetstore.unity.com/packages/tools/modeling/mesh-baker-5017). We highly recommend using Mesh Baker to create texture atlases and combine your meshes. Alternatively, you could write your own or get a license for something like Simplygon.

For Licenses reasons we didn't include [Mesh Baker](https://assetstore.unity.com/packages/tools/modeling/mesh-baker-5017) in the project.

# Conversion to use Addressables
We detailed how we converted the original Asset Streaming project to use Unity Addressables system [here](./ConversionToAddressables.md).

# Horizon Store App
You can find the build version on the [Horizon Store](https://www.meta.com/en-gb/experiences/oculus-asset-streaming-for-unity/7325963400811201/)
