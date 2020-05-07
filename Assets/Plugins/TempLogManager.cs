using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor.Callbacks;

namespace TempConsoleLib
{
    public class TempLogManager
    {
        public static bool HasInited { get; private set; }
        public static readonly Type SelfType = typeof(TempLogManager);

        private static List<LogItem> _logItems = null;
        public static LogItem SelectedItem { get; set; }
        public static readonly string MANAGER_TAG = "[LogManager]";

        private static string[] _logCategoryStrs = null;

        public static int NormalLogCount { get; private set; }
        public static int WarningLogCount { get; private set; }
        public static int ErrorLogCount { get; private set; }

        public static bool IsClearOnPlay = false;
        public static bool IsClearOnBuild = false;
        public static bool IsErrorPause = false;
        public static bool IsShowLog = true;
        public static bool IsShowWarning = true;
        public static bool IsShowError = true;

        public static event Action OnNewLogged;

        #region danger
        private static Type m_entriesType = null;
        private static Type m_entryType = null;
        private static Type m_consoleWindow = null;

        private static Type UnityConsoleWindow
        {
            get
            {
                if (null == m_consoleWindow)
                {
                    m_consoleWindow = System.Type.GetType("UnityEditor.ConsoleWindow, UnityEditor.dll");
                }
                return m_consoleWindow;
            }
        }

        private static Type UnityLogEntry
        {
            get
            {
                if (null == m_entryType)
                {
                    m_entryType = System.Type.GetType("UnityEditor.LogEntry, UnityEditor.dll");
                }
                return m_entryType;
            }
        }

        private static Type UnityLogEntries
        {
            get
            {
                if (null == m_entriesType)
                {
                    m_entriesType = System.Type.GetType("UnityEditor.LogEntries,UnityEditor.dll");
                }
                return m_entriesType;
            }
        }

        public static int GetUnityLogCount()
        {
            return (int)UnityLogEntries.GetMethod("GetCount").Invoke(null, null);
        }

        public static void ClearUnityLogConsole()
        {
            var clearMethod = UnityLogEntries.GetMethod("Clear", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            clearMethod.Invoke(null, null);
        }

        #endregion

        [InitializeOnLoadMethod]
        public static void TryInit()
        {
            _logItems = new List<LogItem>();
            NormalLogCount = 0;
            WarningLogCount = 0;
            ErrorLogCount = 0;

            Application.logMessageReceived -= LogMessageReceived;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.quitting -= OnEditorQuitting;

            Application.logMessageReceived += LogMessageReceived;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.quitting += OnEditorQuitting;

            HasInited = true;

            // to capture error first than warning fianlly info.
            {
                // // enable all log for default console cuz we need to capture all log from it.
                var setall = UnityConsoleWindow.GetMethod("SetFlag", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                setall.Invoke(null, new object[] { 1 << 7, true });
                setall.Invoke(null, new object[] { 1 << 8, true });
                setall.Invoke(null, new object[] { 1 << 9, true });

                // danger
                int startIndex = 0;
                var m = UnityLogEntries.GetMethod("GetEntryInternal", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                var start = UnityLogEntries.GetMethod("StartGettingEntries", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                start.Invoke(null, null);

                int count = GetUnityLogCount();
                LogItem logItem = null;
                int cutIndex = 0;
                for (int i = 0; i < count; i++)
                {
                    if (i >= startIndex)
                    {
                        var e = Activator.CreateInstance(UnityLogEntry);
                        m.Invoke(null, new object[] { i, e });

                        FieldInfo field = e.GetType().GetField("condition", BindingFlags.Instance | BindingFlags.Public);
                        string condition = (string)field.GetValue(e);

                        // field = e.GetType().GetField("instanceID", BindingFlags.Instance | BindingFlags.Public);
                        // int instanceID = (int)field.GetValue(e);

                        field = e.GetType().GetField("mode", BindingFlags.Instance | BindingFlags.Public);
                        int mode = (int)field.GetValue(e);

                        // cutIndex = condition.IndexOf('\n');
                        // string stackTrace = string.Empty;

                        logItem = new LogItem(condition, string.Empty, LogType.Warning);
                        _logItems.Add(logItem);
                    }

                }
                var end = UnityLogEntries.GetMethod("EndGettingEntries", BindingFlags.Static | BindingFlags.Public);
                end.Invoke(null, null);
            }

        }

        public static void CreateLog(in string message, LogType logType)
        {
            string fullMessage = null;

            fullMessage = $"[{MANAGER_TAG}] {message}";

            // create log here
            switch (logType)
            {
                case LogType.Error:
                    Debug.LogError(fullMessage = $"{MANAGER_TAG} {message}");
                    break;
                case LogType.Assert:
                    Debug.LogError(fullMessage = $"{MANAGER_TAG} {message}");
                    break;
                case LogType.Exception:
                    Debug.LogError(fullMessage = $"{MANAGER_TAG} {message}");
                    break;
                case LogType.Warning:
                    Debug.LogWarning(fullMessage = $"{MANAGER_TAG} {message}");
                    break;
                case LogType.Log:
                    Debug.Log(fullMessage = $"{MANAGER_TAG} {message}");
                    break;
                default:
                    Debug.LogError(fullMessage = $"{MANAGER_TAG} {message}");
                    break;
            }
        }

        public static void GetLogs(out List<LogItem> logs)
        {
            logs = new List<LogItem>();
            logs.AddRange(_logItems);
        }

        public static void ClearLogs()
        {
            SelectedItem = null;
            NormalLogCount = 0;
            WarningLogCount = 0;
            ErrorLogCount = 0;
            _logItems.Clear();
            ClearUnityLogConsole();
        }

        private static void OnEditorQuitting()
        {
            Application.logMessageReceived -= LogMessageReceived;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.quitting -= OnEditorQuitting;
        }

        private static void OnPlayModeChanged(PlayModeStateChange obj)
        {

        }

        private static void LogMessageReceived(string message, string stackTrace, LogType type)
        {
            LogItem log = null;
            switch (type)
            {
                case LogType.Error:
                    ErrorLogCount++;
                    break;
                case LogType.Assert:
                    ErrorLogCount++;
                    break;
                case LogType.Warning:
                    WarningLogCount++;
                    break;
                case LogType.Log:
                    NormalLogCount++;
                    break;
                case LogType.Exception:
                    ErrorLogCount++;
                    break;
                default:
                    ErrorLogCount++;
                    break;
            }


            //if (message[0] == '[') // magic 
            if (-1 < message.IndexOf(MANAGER_TAG))
            {
                try
                {
                    // it's our custom log, add it
                    string logCateStr = message;
                    logCateStr = logCateStr.Remove(0, MANAGER_TAG.Length);
                    logCateStr = logCateStr.Substring(logCateStr.IndexOf('[') + 1, logCateStr.IndexOf(']') - logCateStr.IndexOf('[') - 1);
                    log = new LogItem(message, stackTrace, type);
                    _logItems.Add(log);
                }
                catch (Exception e) // 
                {
                    Debug.LogError($"LogManager got a error with custom log {e.InnerException}");
                    log = new LogItem(message, stackTrace, type);
                    _logItems.Add(log);
                }
            }
            else
            {
                // it's default log, add it
                log = new LogItem(message, stackTrace, type);
                _logItems.Add(log);
            }


            OnNewLogged?.Invoke();
        }

        private static void ReceiveTempLog(LogItem logItem)
        {
            _logItems.Add(logItem);
            switch (logItem.GetLogType)
            {
                case LogType.Error:
                    ErrorLogCount++;
                    break;
                case LogType.Assert:
                    ErrorLogCount++;
                    break;
                case LogType.Warning:
                    WarningLogCount++;
                    break;
                case LogType.Log:
                    NormalLogCount++;
                    break;
                case LogType.Exception:
                    ErrorLogCount++;
                    break;
                default:
                    ErrorLogCount++;
                    break;
            }
            OnNewLogged?.Invoke();
        }

        //[OnOpenAssetAttribute(1)]
        //public static bool LeadToCorrectCode1(int instanceID, int line)
        //{
        //    bool result = false;
        //    // if the log jumped into this file, maybe you try to open the code who use LogManager
        //    if (null != SelectedItem)
        //    {
        //        string stackTrace = SelectedItem.LogStackTrace;

        //    }

        //    return result;
        //}

    }

    public static class TempConsoleHelper
    {
        private static string[] m_strings = null;
        public static void GetNumberStr(int number, out string resultStr)
        {
            if (null == m_strings)
            {
                m_strings = new string[101];
                int i = 0;
                while (i < 100)
                {
                    m_strings[i] = i.ToString();
                    i++;
                }
                m_strings[i] = "99+";
            }

            resultStr = m_strings[Mathf.Clamp(number, 0, 100)];
        }

        public static string GetNumberStr(int number)
        {
            string resultStr = string.Empty;
            if (null == m_strings)
            {
                m_strings = new string[101];
                int i = 0;
                while (i < 100)
                {
                    m_strings[i] = i.ToString();
                    i++;
                }
                m_strings[i] = "99+";
            }

            resultStr = m_strings[Mathf.Clamp(number, 0, 100)];
            return resultStr;
        }

        public static bool TryGoToCode(in LogItem logClicked)
        {
            bool result = false;
            string stackTrace = logClicked.LogStackTrace;
            Match matches = null;
            // try to find the top call
            if (!string.IsNullOrEmpty(stackTrace))
            {
                // regular expression check 'at xxx'
                matches = Regex.Match(stackTrace, @"\(at (.+)\)", RegexOptions.IgnoreCase);
                string pathline = "";
                int splitIndex = 0;
                int line = 0;
                if (matches.Success)
                {
                    while (matches.Value.Contains(TempLogManager.SelfType.Name))
                    {
                        matches.NextMatch();
                    }
                    pathline = matches.Groups[1].Value;
                    splitIndex = pathline.LastIndexOf(":");
                    string path = pathline.Substring(0, splitIndex);
                    line = Convert.ToInt32(pathline.Substring(splitIndex + 1));
                    path = $"{Application.dataPath.Substring(0, Application.dataPath.LastIndexOf("Assets"))}{path}";
                    result = UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(path.Replace('/', '\\'), line);
                }
                else
                {
                    // maybe it is a compile log
                    matches = Regex.Match(stackTrace, @".cs", RegexOptions.IgnoreCase);
                    if (matches.Success)
                    {
                        pathline = matches.Groups[1].Value;
                        splitIndex = pathline.LastIndexOf(":");
                        string path = pathline.Substring(0, splitIndex);
                        line = Convert.ToInt32(pathline.Substring(splitIndex + 1));
                        path = $"{Application.dataPath.Substring(0, Application.dataPath.LastIndexOf("Assets"))}{path}";
                        result = UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(path.Replace('/', '\\'), line);
                    }
                }
            }
            else
            {
                // should check log info, maybe it is a compile log
                //matches = Regex.Match(stackTrace, @"/.cs([0-9],[0-9])", RegexOptions.IgnoreCase);
                //int tryFind = stackTrace.IndexOf(".cs");
                //if (tryFind > -1)
                //{
                // find 
                stackTrace = logClicked.LogMessage;
                matches = Regex.Match(stackTrace, @"Assets(\\([a-zA-Z0-9])*)*.cs", RegexOptions.IgnoreCase);
                string pathline = "";
                int splitIndex = 0;
                int line = 0;
                if (matches.Success)
                {
                    pathline = matches.Groups[1].Value;
                    splitIndex = pathline.LastIndexOf(":");
                    string path = pathline.Substring(0, splitIndex);
                    line = Convert.ToInt32(pathline.Substring(splitIndex + 1));
                    path = $"{Application.dataPath.Substring(0, Application.dataPath.LastIndexOf("Assets"))}{path}";
                    result = UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(path.Replace('/', '\\'), line);
                }
                else
                {
                    // it is not a log you can go into deep
                    return true;
                }


            }
            return result;
        }

    }

    [Serializable]
    public class TempLog
    {
        public string Time = null;
        public string Info = null;
        public string Detail = null;
        public int LogType = 0;
    }

}