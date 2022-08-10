// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-AssetStreaming/tree/main/Assets/AssetStreaming/LICENSE

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

public class Cleanup : EditorWindow
{
    [MenuItem("Window/Cleanup")]
    static void Init()
    {
        Cleanup window = (Cleanup)EditorWindow.GetWindow(typeof(Cleanup));
        window.Show();
    }

    void OnGUI()
    {
        if(GUILayout.Button("Clean"))
        {
            List<GameObject> objects = new List<GameObject>(FindObjectsOfType<GameObject>());
            objects.RemoveAll(x => x.name != "Colliders");
            foreach (GameObject go in objects)
                RemoveNonPhysicsComponents(go);
        }
    }

    private void RemoveNonPhysicsComponents(GameObject go)
    {
        Component[] components = go.GetComponents<Component>();
        for (int i = 0; i < components.Length; ++i)
        {
            Type type = components[i].GetType();
            if (!type.IsSubclassOf(typeof(Collider)) && type != typeof(Transform))
            {
                DestroyImmediate(components[i]);
            }
            else
            {
                Behaviour behaviour = components[i] as Behaviour;
                if (behaviour != null)
                {
                    if (!behaviour.isActiveAndEnabled)
                    {
                        DestroyImmediate(components[i]);
                    }
                }
            }
        }

        for (int i = 0; i < go.transform.childCount; ++i)
        {
            RemoveNonPhysicsComponents(go.transform.GetChild(i).gameObject);
        }
    }
}
