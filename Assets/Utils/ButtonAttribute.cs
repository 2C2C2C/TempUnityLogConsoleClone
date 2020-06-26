using System;

[AttributeUsage(AttributeTargets.Method)]
public class ButtonAttribute : Attribute
{
    public string m_methodName = string.Empty;
    public ButtonAttribute(string methodName)
    {
        m_methodName = methodName;
    }
}
