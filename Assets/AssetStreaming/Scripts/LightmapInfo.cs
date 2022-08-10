// Copyright (c) Meta Platforms, Inc. and affiliates.
using UnityEngine;

// Serializes the lightmap information of meshes not managed by the LOD manager.
// Used to combine the lightmaps sublevels into a texture array in SublevelCombinerEditor.cs

public class LightmapInfo : MonoBehaviour
{
    public Texture2D lightmap;
    public Vector4 lightmapScaleOffset;
}
