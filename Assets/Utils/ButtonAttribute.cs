using UnityEngine;
using System.Collections;
using System;

[AttributeUsage(AttributeTargets.Method)]
public class ButtonAttribute : Attribute
{
    public string m_name;
    public ButtonAttribute(string methodName)
    {
        m_name = methodName;
    }
}
