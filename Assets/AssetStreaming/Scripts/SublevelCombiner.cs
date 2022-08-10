// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-AssetStreaming/tree/main/Assets/AssetStreaming/LICENSE

using UnityEngine;

// Component serializes the batched lightmap texture array.
// Makes the lightmap available in the shaders.

[ExecuteInEditMode]
public class SublevelCombiner : MonoBehaviour
{
    public Texture2DArray batchedLightmap;

    private void OnEnable()
    {
        if(batchedLightmap != null)
            Shader.SetGlobalTexture("BatchedLightmap", batchedLightmap);
    }
}
