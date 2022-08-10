// Copyright (c) Meta Platforms, Inc. and affiliates.
using System.Collections.Generic;
using UnityEngine;

// A set of waypoints to create a path & logic to get a point on that path.

public class Benchmark : MonoBehaviour
{
    public List<Transform> waypoints;
    private List<float> waypointDistances;
    private float totalDistance;

    private void Start()
    {
        waypointDistances = new List<float>();

        totalDistance = 0.0f;
        for(int i = 0; i < waypoints.Count; ++i)
        {
            Vector3 p0 = waypoints[i].position;
            Vector3 p1 = waypoints[(i + 1) % waypoints.Count].position;
            float length = (p0 - p1).magnitude;
            waypointDistances.Add(totalDistance);
            totalDistance += length;
        }
    }

    public Vector3 GetPoint(float progress)
    {
        progress %= totalDistance;
        int waypoint = waypointDistances.FindIndex(x => x > progress);
        if (waypoint < 0)
            waypoint = waypointDistances.Count - 1;
        else
        {
            waypoint -= 1;
            if (waypoint < 0)
                waypoint += waypointDistances.Count;
        }

        float delta = progress - waypointDistances[waypoint];
        Vector3 p0 = waypoints[waypoint].position;
        Vector3 p1 = waypoints[(waypoint + 1) % waypoints.Count].position;
        return p0 + (p1 - p0).normalized * delta;
    }
}
