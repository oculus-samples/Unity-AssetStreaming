// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-AssetStreaming/tree/main/Assets/AssetStreaming/LICENSE

using UnityEngine;

public class LODGenerator : MonoBehaviour
{
    public Vector2 cellSize = new Vector2(20, 20);
    public float lowestPointTerrain = 0.0f;
    public int lodLevels = 3;
    public float[] lodMinObjectRadius = new float[3] { 0.0f, 2.5f, 15.0f };
    public float[] lodDecimationPercentage = new float[3] { 100.0f, 75.0f, 50.0f };
    public GameObject[] additionalObjects;
}
