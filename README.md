# AssetStreaming

AssetStreaming is a Unity project that demonstrates efficient asset streaming techniques for open-world games. It handles world loading without loading everything into memory at once.

This codebase serves as a reference and template for implementing asset streaming in your projects. You can test it on the [Meta Store - AssetStreaming](https://www.meta.com/en-gb/experiences/oculus-asset-streaming-for-unity/7325963400811201/).

## Project Description

This project showcases advanced asset streaming techniques, including world loading optimization and memory management. It efficiently loads assets without overwhelming system resources.

Built with the [Unity engine](https://unity.com/) and Unity's Addressables system, the project demonstrates practical asset streaming for open-world environments. It provides tools and methods for efficient resource management.

## How to Run the Project in Unity

1. Use *Unity 6000.0.50f1* or newer.
2. Load the [Assets/AssetStreaming/Scenes/Startup](Assets/AssetStreaming/Scenes/Startup.unity) scene.
3. To build to a device, follow these steps:
   - Build the Addressables assets from Window->Asset Management->Addressables->Groups.
   - Use Build Settings to build the APK.
   - Alternatively, use the AssetStreaming menu for automated builds:
     - **Build Addressables and APK**: Build both Addressables and APK.
     - **Build Addressables**: Build only the Addressables.
     - **Build APK**: Build only the APK (useful when only changing code).

## Dependencies

This project uses the following plugins and software:

- [Unity](https://unity.com/download) 6000.0.50f1 or newer.
- Unity Addressables System.
- [Mesh Baker](https://assetstore.unity.com/packages/tools/modeling/mesh-baker-5017) (recommended for LOD generation; not included due to licensing).

Alternative LOD solutions:
- Custom mesh optimization tools.
- Simplygon (commercial license required).

## Getting the Code

Ensure you have Git LFS installed by running:

```sh
git lfs install
```

Then, clone this repository using the "Code" button above or this command:

```sh
git clone https://github.com/oculus-samples/Unity-AssetStreaming.git
```

## Documentation

More information is available in the project documentation:

- [Guide to Asset Streaming in Open World Games](https://developers.meta.com/horizon/blog/now-available-guide-to-asset-streaming-in-open-world-games-/).
- [Asset Streaming Documentation](https://developers.meta.com/horizon/documentation/unity/po-assetstreaming/).
- [Conversion to Addressables](./ConversionToAddressables.md).

## License

The majority of AssetStreaming is licensed under the [MIT LICENSE](./LICENSE). However, the files listed below are licensed under their respective terms:

- [TextMeshPro](./Assets/TextMesh Pro) - [Unity Companion License](https://unity.com/legal/licenses/unity-companion-license).
- [Demo Assets](./Assets/DemoAssets/) - [Meta Platforms Technologies Examples License](./Assets/DemoAssets/LICENSE.txt).

## Contribution

See the [CONTRIBUTING](./CONTRIBUTING.md) file for information on how to contribute.
