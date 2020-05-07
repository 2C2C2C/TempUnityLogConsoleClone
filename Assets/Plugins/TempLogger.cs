using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using TempConsoleLib;
using UnityEngine;

public class TempLogger
{
    public static void OpenFileTest()
    {
        string fileName = "D:/Project/Unity/TempLogManager/Assets/Scripts/LogTester01.cs";
        fileName.Replace('/', '\\');
        bool result = UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(fileName, 20);
        Debug.Log($"open file resut {result}");
    }


    private static string GetStackTrace()
    {
        // 找到类UnityEditor.ConsoleWindow
        var typeConsoleWindow = typeof(UnityEditor.EditorWindow).Assembly.GetType("UnityEditor.ConsoleWindow");
        // 找到UnityEditor.ConsoleWindow中的成员ms_ConsoleWindow
        var filedInfo = typeConsoleWindow.GetField("ms_ConsoleWindow", BindingFlags.Static | BindingFlags.NonPublic);
        // 获取ms_ConsoleWindow的值
        var ConsoleWindowInstance = filedInfo.GetValue(null);
        if (ConsoleWindowInstance != null)
        {
            if ((object)UnityEditor.EditorWindow.focusedWindow == ConsoleWindowInstance)
            {
                var typeListViewState = typeof(UnityEditor.EditorWindow).Assembly.GetType("UnityEditor.ListViewState");
                // 找到类UnityEditor.ConsoleWindow中的成员m_ListView
                filedInfo = typeConsoleWindow.GetField("m_ListView", BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var listView = filedInfo.GetValue(ConsoleWindowInstance);

                // 下面是stacktrace中一些可能有用的数据、函数和使用方法，这里就不一一说明了，我们这里暂时还用不到
                filedInfo = typeListViewState.GetField("row", BindingFlags.Instance | BindingFlags.Public);
                int row = (int)filedInfo.GetValue(listView);
                // 找到类UnityEditor.ConsoleWindow中的成员m_ActiveText
                filedInfo = typeConsoleWindow.GetField("m_ActiveText", BindingFlags.Instance | BindingFlags.NonPublic);
                string activeText = filedInfo.GetValue(ConsoleWindowInstance).ToString();
                return activeText;
            }
        }

        return null;
    }

}
