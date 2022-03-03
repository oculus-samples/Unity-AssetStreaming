// Copyright (c) Meta Platforms, Inc. and affiliates.
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
