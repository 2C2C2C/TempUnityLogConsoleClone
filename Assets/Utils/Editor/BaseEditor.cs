using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UnityEngine.Object), true), CanEditMultipleObjects]
public class BaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
                base.OnInspectorGUI();

        // Added functionality
        Type type = target.GetType();
        while (type != null)
        {
            MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            foreach (MethodInfo method in methods)
            {
                ButtonAttribute button = method.GetCustomAttribute<ButtonAttribute>();
                if (button != null && method.GetParameters().Length > 0)
                {
                    EditorGUILayout.HelpBox("ButtonAttribute: method cannot have parameters.", MessageType.Warning);
                }
                else if (button != null && GUILayout.Button(button.name))
                {
                    method.Invoke(target, new object[] { });
                }
            }
            type = type.BaseType;
        }

    }
}
