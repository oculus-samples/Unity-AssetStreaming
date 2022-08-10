// Copyright (c) Meta Platforms, Inc. and affiliates.
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
