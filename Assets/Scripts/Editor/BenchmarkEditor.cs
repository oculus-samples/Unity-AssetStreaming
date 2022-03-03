// Copyright (c) Meta Platforms, Inc. and affiliates.
using UnityEngine;
using UnityEditor;

// Tool for creating a path by placing waypoints.

[CustomEditor(typeof(Benchmark))]
public class BenchmarkEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        Benchmark benchmark = (Benchmark)target;
        if(GUILayout.Button("Add Waypoint"))
        {
            GameObject go = new GameObject("waypoint");
            go.transform.parent = benchmark.transform;
            benchmark.waypoints.Add(go.transform);
            if(benchmark.waypoints.Count > 2)
                go.transform.position = benchmark.waypoints[benchmark.waypoints.Count - 2].position;

            Selection.objects = new Object[] { go };
        }

        for(int i = 0; i < benchmark.waypoints.Count; ++i)
        {
            if(benchmark.waypoints[i] == null)
            {
                benchmark.waypoints.RemoveAt(i);
                --i;
                continue;
            }

            RaycastHit hit = new RaycastHit();
            if(Physics.Raycast(benchmark.waypoints[i].position, Vector3.down, out hit))
            {
                benchmark.waypoints[i].position = hit.point + Vector3.up * 1.0f;
            }
        }
    }

    public void OnSceneGUI()
    {
        Benchmark benchmark = (Benchmark)target;
        if(benchmark.waypoints.Count > 1)
        {
            Vector3[] points = new Vector3[benchmark.waypoints.Count];
            int[] segments = new int[benchmark.waypoints.Count * 2 + 2];
            for(int i = 0; i < benchmark.waypoints.Count; ++i)
            {
                points[i] = benchmark.waypoints[i].position;
                segments[i * 2] = i;
                segments[i * 2 + 1] = (i + 1) % benchmark.waypoints.Count;
            }
            Handles.DrawLines(points, segments);
        }
    }
}
