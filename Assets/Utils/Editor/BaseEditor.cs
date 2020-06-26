using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MonoBehaviour), true), CanEditMultipleObjects]
public class BaseEditor : Editor
{
    private Type m_targetType = null;

    public override void OnInspectorGUI()
    {
        // draw default stuff
        base.OnInspectorGUI();

        if (null == m_targetType)
            m_targetType = target.GetType();

        while (m_targetType != null)
        {
            // try find member function and static function :)
            MethodInfo[] methods = m_targetType.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            foreach (var method in methods)
            {
                ButtonAttribute button = method.GetCustomAttribute<ButtonAttribute>();
                if (button != null && method.GetParameters().Length > 0)
                {
                    EditorGUILayout.HelpBox("ButtonAttribute: method cannot have parameters.", MessageType.Warning);
                }
                else if (button != null && GUILayout.Button(button.m_methodName))
                {
                    method.Invoke(target, new object[] { });
                }
            }
            m_targetType = m_targetType.BaseType;
        }

    }
}
