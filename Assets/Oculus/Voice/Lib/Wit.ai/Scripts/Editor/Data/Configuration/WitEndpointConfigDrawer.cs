/*
 * Copyright (c) Facebook, Inc. and its affiliates.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using UnityEditor;
using UnityEngine;

namespace Facebook.WitAi.Configuration
{

    [CustomPropertyDrawer(typeof(WitEndpointConfig))]
    public class WitEndpointConfigDrawer : PropertyDrawer
    {
        private string editing;
        private Vector2 scroll;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 0;
        }

        private void DrawProperty(SerializedProperty propery, string name,
            string label, string defaultValue)
        {
            var propValue = propery.FindPropertyRelative(name);
            GUILayout.BeginHorizontal();
            if (editing == name)
            {

                EditorGUILayout.PropertyField(propValue, new GUIContent(label));

                WitStyles.ResetIcon.tooltip = $"Reset to default values ({defaultValue})";
                if (GUILayout.Button(WitStyles.ResetIcon, WitStyles.ImageIcon))
                {
                    editing = string.Empty;

                    switch (propValue.type)
                    {
                        case "string":
                            propValue.stringValue = defaultValue;
                            break;
                        case "int":
                            propValue.intValue = int.Parse(defaultValue);
                            break;
                    }
                }

                if (GUILayout.Button(WitStyles.AcceptIcon, WitStyles.ImageIcon))
                {
                    editing = string.Empty;
                }
            }
            else
            {
                switch (propValue.type)
                {
                    case "string":
                        defaultValue = string.IsNullOrEmpty(propValue.stringValue)
                            ? defaultValue
                            : propValue.stringValue;
                        break;
                    case "int":
                        defaultValue = propValue.intValue.ToString();
                        break;
                }

                EditorGUI.BeginDisabledGroup(editing != name);
                EditorGUILayout.TextField(label, defaultValue);
                EditorGUI.EndDisabledGroup();

                if (GUILayout.Button(WitStyles.EditIcon, WitStyles.ImageIcon))
                {
                    if (editing == name)
                    {
                        editing = string.Empty;
                    }
                    else
                    {
                        editing = name;
                    }
                }
            }

            GUILayout.EndHorizontal();
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(100));
            DrawProperty(property, "uriScheme", "Uri Scheme", WitRequest.URI_SCHEME);
            DrawProperty(property, "authority", "Host", WitRequest.URI_AUTHORITY);
            DrawProperty(property, "port", "Port", "80");
            DrawProperty(property, "witApiVersion", "Wit Api Version", WitRequest.WIT_API_VERSION);
            DrawProperty(property, "speech", "Speech", WitRequest.WIT_ENDPOINT_SPEECH);
            DrawProperty(property, "message", "Message", WitRequest.WIT_ENDPOINT_MESSAGE);
            GUILayout.EndScrollView();
        }
    }
}
