// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
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
