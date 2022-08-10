// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-AssetStreaming/tree/main/Assets/AssetStreaming/LICENSE

using UnityEngine;

[ExecuteInEditMode]
public class TimeOfDay : MonoBehaviour
{
    void Start()
    {
        Shader.SetGlobalColor(Shader.PropertyToID("_TimeOfDayWorldTint"), Color.white);
        Shader.SetGlobalColor(Shader.PropertyToID("_TimeOfDayTerrainTint"), Color.white);
        // Make sure we update the environment as we start to get the right skybox info
        DynamicGI.UpdateEnvironment();
    }
}
