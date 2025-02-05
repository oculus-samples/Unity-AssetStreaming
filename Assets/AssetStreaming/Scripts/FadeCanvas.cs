// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-AssetStreaming/tree/main/Assets/AssetStreaming/LICENSE

using UnityEngine;

// Shows canvas on startup or when the player presses a button.
public class FadeCanvas : MonoBehaviour
{
    private CanvasGroup canvas;
    private float timer = 10.0f;

    void Start()
    {
        canvas = GetComponentInChildren<CanvasGroup>();
    }

    void Update()
    {
        if (timer > 0.0f)
        {
            timer -= Time.deltaTime;
            canvas.alpha = timer;
        }

        if (OVRInput.Get(OVRInput.RawButton.B) || OVRInput.Get(OVRInput.RawButton.X))
            timer = 10.0f;
    }
}
