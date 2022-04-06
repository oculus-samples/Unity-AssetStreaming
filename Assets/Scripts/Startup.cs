// Copyright (c) Meta Platforms, Inc. and affiliates.
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
