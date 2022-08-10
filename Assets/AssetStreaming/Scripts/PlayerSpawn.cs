// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-AssetStreaming/tree/main/Assets/AssetStreaming/LICENSE

using UnityEngine;

// Stores the player spawn position & resets the player position if he leaves the level.
public class PlayerSpawn : MonoBehaviour
{
    public float killZ = 0.0f;

    public Vector3 spawnPosition;

    void Start()
    {
        spawnPosition = transform.position;
    }
    
    void Update()
    {
        if(transform.position.y < killZ)
        {
            transform.position = spawnPosition;
        }
    }

    private void OnDrawGizmosSelected()
    {
        spawnPosition = transform.position;
    }
}
