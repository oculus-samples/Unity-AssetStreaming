// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-AssetStreaming/tree/main/Assets/AssetStreaming/LICENSE

using UnityEngine;
using UnityEngine.AddressableAssets;

public class Startup : MonoBehaviour
{
    [SerializeField]
    private AssetReference nextScene = null;
    void Start()
    {
        nextScene.LoadSceneAsync();
    }
}
