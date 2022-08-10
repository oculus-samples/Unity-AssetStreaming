// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-AssetStreaming/tree/main/Assets/AssetStreaming/LICENSE

using UnityEngine;

// Serializes the lightmap information of meshes not managed by the LOD manager.
// Used to combine the lightmaps sublevels into a texture array in SublevelCombinerEditor.cs

public class LightmapInfo : MonoBehaviour
{
    public Texture2D lightmap;
    public Vector4 lightmapScaleOffset;
}
