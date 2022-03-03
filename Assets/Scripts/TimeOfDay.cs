// Copyright (c) Meta Platforms, Inc. and affiliates.
using UnityEngine;

[ExecuteInEditMode]
public class TimeOfDay : MonoBehaviour
{
    void Start()
    {
        Shader.SetGlobalColor(Shader.PropertyToID("_TimeOfDayWorldTint"), Color.white);
        Shader.SetGlobalColor(Shader.PropertyToID("_TimeOfDayTerrainTint"), Color.white);
    }
}
