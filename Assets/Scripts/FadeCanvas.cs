// Copyright (c) Meta Platforms, Inc. and affiliates.
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
        timer -= Time.deltaTime;
        canvas.alpha = timer;

        if (OVRInput.Get(OVRInput.RawButton.B) || OVRInput.Get(OVRInput.RawButton.X))
            timer = 10.0f;
    }
}
