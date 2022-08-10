// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-AssetStreaming/tree/main/Assets/AssetStreaming/LICENSE

using UnityEngine;

public class RefreshRate : MonoBehaviour
{
    void Start()
    {
        // Ensure we have a display
        if (OVRManager.display == null)
        {
            return;
        }
        float[] frequencies = OVRManager.display.displayFrequenciesAvailable;
        float highest = 0.0f;
        foreach(float f in frequencies)
        {
            if (f > highest)
                highest = f;
        }

        OVRManager.display.displayFrequency = highest;
    }
}
