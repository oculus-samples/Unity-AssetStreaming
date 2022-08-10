// Copyright (c) Meta Platforms, Inc. and affiliates.
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
