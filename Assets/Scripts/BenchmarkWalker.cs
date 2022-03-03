// Copyright (c) Meta Platforms, Inc. and affiliates.
using UnityEngine;

public class BenchmarkWalker : MonoBehaviour
{
    public Benchmark benchmark;

    public float speed = 1.0f;
    private float progress;

    private void OnEnable()
    {
        progress = 0.0f;
        Rigidbody rb = GetComponent<Rigidbody>();
        if(rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    private void OnDisable()
    {
        progress = 0.0f;
        Rigidbody rb = GetComponent<Rigidbody>();
        if(rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }
    }

    private void Update()
    {
        float step = speed * Time.deltaTime;
        progress += step;
        if (step >= 1.0f)
            step = 0.99f;
        Vector3 point = benchmark.GetPoint(progress + 1.0f);
        Vector3 diff = point - transform.position;
        float len = diff.magnitude;
        Vector3 dir = diff / len;
        transform.rotation = Quaternion.LookRotation(new Vector3(dir.x, 0.0f, dir.z), Vector3.up);
        transform.Translate(dir * len * step, Space.World);
        Debug.DrawLine(transform.position, point, Color.red);
    }
}
