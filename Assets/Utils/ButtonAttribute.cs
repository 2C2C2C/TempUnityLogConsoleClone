using UnityEngine;
using System.Collections;
using System;

[AttributeUsage(AttributeTargets.Method)]
public class ButtonAttribute : Attribute
{
    public string name;
    public ButtonAttribute(string name)
    {
        this.name = name;
    }
}
