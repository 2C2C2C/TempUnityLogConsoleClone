﻿using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System.Text.RegularExpressions;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Compilation;
#endif

namespace CustomLog
{
    public static class TempLogManager
    {
        public static bool HasInited { get; private set; }

        public static readonly string MANAGER_TAG = "[LogManager]";
        public static readonly string MANAGER_NAME = "LogManager";

        public static int NormalLogCount { get; private set; }
        public static int WarningLogCount { get; private set; }
        public static int ErrorLogCount { get; private set; }

        private static StreamWriter m_logFileWriter = null;
        private static readonly string LOG_FILE_NAME = "LogFile";

        private static string[] m_logCategoryStrs = null;

#if UNITY_EDITOR

        #region variables for editor

        public static int PrevShowLogCount { get; private set; }
        public static int CurrentShowLogCount { get; private set; }

        public static readonly Type SelfType = typeof(TempLogManager);

        private static List<TempLogItem> m_logItems = null;
        public static TempLogItem SelectedItem { get; set; }

        public static bool IsClearOnPlay = false;
        public static bool IsClearOnBuild = false;
        public static bool IsErrorPause = false;
        public static bool IsShowLog = true;
        public static bool IsShowWarning = true;
        public static bool IsShowError = true;

        public static event Action OnLogItemCreated;

        #region to get unity console and logs

        public static readonly int LOG_FLAG = 1 << 7;
        public static readonly int WARNING_FLAG = 1 << 8;
        public static readonly int ERROR_FLAG = 1 << 9;
        private static int m_logFlags = 0;

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

        #endregion

#endif

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#endif
        public static void InitLogManager()
        {
            HasInited = false;
            Application.logMessageReceived -= LogMessageReceived;
            Application.logMessageReceived += LogMessageReceived;

            if (!Application.isEditor)
            {
                Application.quitting += OnGameQuit;
                FreshFileWriter();
            }

            m_logFlags = LOG_FLAG | WARNING_FLAG | ERROR_FLAG;

            NormalLogCount = 0;
            WarningLogCount = 0;
            ErrorLogCount = 0;

#if UNITY_EDITOR
            InitLogmanagerForEditor();
#endif

            HasInited = true;
        }

        public static void Log(in string message)
        {
            CreateLog(message, LogType.Log);
        }

        public static void Log(in string message, params object[] args)
        {
            CreateLog(string.Format(message, args), LogType.Log);
        }

        public static void LogWarning(in string message)
        {
            CreateLog(message, LogType.Warning);
        }

        public static void LogWarning(in string message, params object[] args)
        {
            CreateLog(string.Format(message, args), LogType.Warning);
        }

        public static void LogError(in string message)
        {
            CreateLog(message, LogType.Error);
        }

        public static void LogError(in string message, params object[] args)
        {
            CreateLog(string.Format(message, args), LogType.Error);
        }

        public static void CreateLog(in string message, LogType logType)
        {
            string fullMessage = null;
            fullMessage = $"{MANAGER_TAG} {message}";

            // create log here
            switch (logType)
            {
                case LogType.Error:
                    Debug.LogError(fullMessage);
                    break;
                case LogType.Assert:
                    Debug.LogError(fullMessage);
                    break;
                case LogType.Exception:
                    Debug.LogError(fullMessage);
                    break;
                case LogType.Warning:
                    Debug.LogWarning(fullMessage);
                    break;
                case LogType.Log:
                    Debug.Log(fullMessage);
                    break;
                default:
                    Debug.LogError(fullMessage);
                    break;
            }
        }

        public static int GetLogFlagByType(LogType logType)
        {
            int result = 0;
            switch (logType)
            {
                case LogType.Error:
                case LogType.Assert:
                    result = ERROR_FLAG;
                    break;
                case LogType.Warning:
                    result = WARNING_FLAG;
                    break;
                case LogType.Log:
                    result = LOG_FLAG;
                    break;
                case LogType.Exception:
                    result = ERROR_FLAG;
                    break;
                default:
                    result = LOG_FLAG;
                    break;
            }
            return result;
        }

        #region methods for editor

#if UNITY_EDITOR
        // here are some methods only used in editor

        public static void ClearLogs()
        {
            SelectedItem = null;
            NormalLogCount = 0;
            WarningLogCount = 0;
            ErrorLogCount = 0;
            m_logItems.Clear();
            ClearUnityLogConsole();
        }

        public static void GetLogs(out List<TempLogItem> logs)
        {
            PrevShowLogCount = CurrentShowLogCount;
            CurrentShowLogCount = 0;
            // too bad
            logs = new List<TempLogItem>();
            for (int i = 0; i < m_logItems.Count; i++)
            {
                if (m_logItems[i].GetLogTypeFlag == (m_logItems[i].GetLogTypeFlag & m_logFlags))
                {
                    CurrentShowLogCount++;
                    logs.Add(m_logItems[i]);
                }
            }
        }

        public static void SetShowingLogFlag(bool enableLog, bool enableWarning, bool enableError)
        {
            m_logFlags = 0;
            if (enableLog)
                m_logFlags = m_logFlags | LOG_FLAG;
            if (enableWarning)
                m_logFlags = m_logFlags | WARNING_FLAG;
            if (enableError)
                m_logFlags = m_logFlags | ERROR_FLAG;
        }

        private static void OnPlayModeChanged(PlayModeStateChange playMode)
        {
            if (PlayModeStateChange.EnteredPlayMode == playMode && IsClearOnPlay)
            {
                ClearLogs();
            }

            if (PlayModeStateChange.EnteredPlayMode == playMode)
            {
                FreshFileWriter();
            }

            if (PlayModeStateChange.ExitingPlayMode == playMode)
            {
                CloseFileWriter();
            }
        }

        private static void OnEditorQuitting()
        {
            CompilationPipeline.assemblyCompilationStarted -= OnCodeCompileStart;
            Application.logMessageReceived -= LogMessageReceived;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.quitting -= OnEditorQuitting;
        }

        private static void OnCodeCompileStart(string obj)
        {
            ClearUnityLogConsole();
            ClearLogs();
        }

        private static void InitLogmanagerForEditor()
        {
            m_logItems = new List<TempLogItem>();

            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.quitting -= OnEditorQuitting;

            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.quitting += OnEditorQuitting;

            GetLogsFromUnityConsole(LogType.Log);
            GetLogsFromUnityConsole(LogType.Warning);
            GetLogsFromUnityConsole(LogType.Error);

            CompilationPipeline.assemblyCompilationStarted += OnCodeCompileStart;
        }

        private static void GetLogsFromUnityConsole(LogType logType)
        {
            int enableLogFlag = 0;

            switch (logType)
            {
                case LogType.Error:
                case LogType.Assert:
                    enableLogFlag = ERROR_FLAG;
                    break;
                case LogType.Warning:
                    enableLogFlag = WARNING_FLAG;
                    break;
                case LogType.Log:
                    enableLogFlag = LOG_FLAG;
                    break;
                case LogType.Exception:
                    enableLogFlag = ERROR_FLAG;
                    break;
                default:
                    enableLogFlag = LOG_FLAG | WARNING_FLAG | ERROR_FLAG;
                    logType = LogType.Log;
                    break;
            }

            // to capture the logs already in Unity Console
            // enable all log for default console cuz we need to capture all log from it.
            var setall = UnityConsoleWindow.GetMethod("SetFlag", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            setall.Invoke(null, new object[] { LOG_FLAG, false });
            setall.Invoke(null, new object[] { WARNING_FLAG, false });
            setall.Invoke(null, new object[] { ERROR_FLAG, false });
            setall.Invoke(null, new object[] { enableLogFlag, true });

            // danger
            int startIndex = 0;
            var m = UnityLogEntries.GetMethod("GetEntryInternal", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            var start = UnityLogEntries.GetMethod("StartGettingEntries", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            start.Invoke(null, null);

            int count = GetUnityLogCount();
            TempLogItem logItem = null;
            for (int i = 0; i < count; i++)
            {
                if (i >= startIndex)
                {
                    var e = Activator.CreateInstance(UnityLogEntry);
                    m.Invoke(null, new object[] { i, e });

                    FieldInfo field = e.GetType().GetField("condition", BindingFlags.Instance | BindingFlags.Public);
                    string condition = (string)field.GetValue(e);

                    logItem = new TempLogItem(condition, string.Empty, logType, enableLogFlag);
                    m_logItems.Add(logItem);
                }

            }
            var end = UnityLogEntries.GetMethod("EndGettingEntries", BindingFlags.Static | BindingFlags.Public);
            end.Invoke(null, null);

            setall.Invoke(null, new object[] { LOG_FLAG, true });
            setall.Invoke(null, new object[] { WARNING_FLAG, true });
            setall.Invoke(null, new object[] { ERROR_FLAG, true });
        }
#endif
        #endregion

        private static void LogMessageReceived(string message, string stackTrace, LogType type)
        {
            TempLogItem log = null;
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

            int logFlag = GetLogFlagByType(type);
            if (-1 < message.IndexOf(MANAGER_TAG))
            {
                try
                {
                    // it's our custom log, add it
                    log = new TempLogItem(message, stackTrace, type, logFlag);
                }
                catch (Exception e) // 
                {
                    Debug.LogError($"LogManager got a error with custom log {e.InnerException}");
                    log = new TempLogItem(message, stackTrace, type, logFlag);
                }
            }
            else
            {
                // it's default log, add it
                log = new TempLogItem(message, stackTrace, type, logFlag);
            }

            if (null != log)
            {
#if UNITY_EDITOR
                m_logItems.Add(log);
                OnLogItemCreated?.Invoke();
#endif
                WriteLogToFile(log);
            }
        }

        private static void FreshFileWriter()
        {
            if (null != m_logFileWriter)
                CloseFileWriter();

            string path = null;
            path = $"{Application.persistentDataPath}/{LOG_FILE_NAME}_{System.DateTime.Now.ToString("yyyy-mm-dd_HH-mm-ss")}.txt";
            m_logFileWriter = new StreamWriter(File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite), System.Text.UTF8Encoding.Default);
            m_logFileWriter.WriteLine($"Game launch");
            m_logFileWriter.WriteLine($"Time : {System.DateTime.Now.ToString()}");
            m_logFileWriter.WriteLine($"----------------------------------------\n");
        }

        private static void CloseFileWriter()
        {
            if (null != m_logFileWriter)
            {
                m_logFileWriter.WriteLine($"Game End at {System.DateTime.Now.ToString("yyyy-mm-dd_HH-mm-ss")}");
                m_logFileWriter.WriteLine($"Result:\n log : {NormalLogCount}\n warning : {WarningLogCount}\n error : {ErrorLogCount}\n");
                m_logFileWriter.Dispose();
                m_logFileWriter.Close();
            }
            m_logFileWriter = null;
        }

        private static void WriteLogToFile(in TempLogItem logItem)
        {
#if UNITY_EDITOR
            if (Application.isEditor && !EditorApplication.isPlaying)
            {
                return;
            }
#endif

            if (Application.isPlaying)
            {
                if (m_logFileWriter == null)
                {
                    FreshFileWriter();
                }

                // write log to file
                m_logFileWriter.WriteLine($"{logItem.GetLogType}\n{logItem.LogItme}\n{logItem.LogMessage}\n\n{logItem.LogStackTrace}");
                m_logFileWriter.WriteLine("-----------------------------\n");
            }
        }

        private static void OnGameQuit()
        {
            Application.logMessageReceived -= LogMessageReceived;
            Application.quitting -= OnGameQuit;
            CloseFileWriter();
        }

    }

#if UNITY_EDITOR
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

        public static bool TryGoToCode(in TempLogItem logClicked)
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
                        stackTrace = stackTrace.Remove(0, matches.Index + matches.Length);
                        matches = Regex.Match(stackTrace, @"\(at (.+)\)", RegexOptions.IgnoreCase);
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
                stackTrace = logClicked.LogMessage;
                matches = Regex.Match(stackTrace, @"Assets(\\([a-zA-Z0-9])*)*.cs\(([0-9]*,[0-9]*)\)", RegexOptions.IgnoreCase);
                string pathline = "";
                int line = 0;
                if (matches.Success)
                {
                    //pathline = matches.Groups[1].Value;
                    pathline = matches.Value;
                    int lineNumLen = pathline.LastIndexOf(",") - pathline.LastIndexOf("(") - 1;
                    Int32.TryParse(pathline.Substring(pathline.LastIndexOf("(") + 1, lineNumLen), out line);
                    string path = $"{Application.dataPath.Substring(0, Application.dataPath.LastIndexOf("Assets"))}{pathline.Remove(pathline.LastIndexOf("("))}";
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


        private static string[] m_mutilineStackTrace = null;
        private static string m_tempStackTrace = null;

        // TODO : why shit PC OS has these 2 kinda of shit
        // to match "Assets\Plugins\LogPack\TempLogManager.cs(620,25)"
        public static readonly string REGEX_MATCH_PAT_1 = @"Assets\\.*cs\([0-9,]*\)";

        // to match "at Assets/Utils/Editor/BaseEditor.cs:28"
        public static readonly string REGEX_MATCH_PAT_2 = @"Assets/.*cs:[0-9]*";
        private static Match m_mathObject = null;
        public static void GetTopFileOfCallStack(in string callstack, out string filePath, out int lineNum)
        {
            filePath = string.Empty;
            lineNum = 0;
            m_tempStackTrace = string.Copy(callstack);

            m_mutilineStackTrace = callstack.Split('\n');

            string tempLineStr = null;

            for (int i = 0; i < m_mutilineStackTrace.Length; i++)
            {
                m_mathObject = Regex.Match(m_mutilineStackTrace[i], REGEX_MATCH_PAT_1, RegexOptions.IgnoreCase);

                if (m_mathObject.Success)
                {
                    // value like Assets\Plugins\LogPack\TempLogManager.cs(620,25)
                    if (m_mathObject.Value.Contains(TempLogManager.MANAGER_NAME))
                        continue;

                    filePath = m_mathObject.Value.Substring(0, m_mathObject.Value.IndexOf('('));
                    lineNum = m_mathObject.Value.IndexOf(',') - m_mathObject.Value.IndexOf('(') - 1;
                    tempLineStr = m_mathObject.Value.Substring(m_mathObject.Value.IndexOf('(') + 1, lineNum);
                    if (!Int32.TryParse(tempLineStr, out lineNum))
                        lineNum = 1;
                    break;
                }

                m_mathObject = Regex.Match(m_mutilineStackTrace[i], REGEX_MATCH_PAT_2, RegexOptions.IgnoreCase);
                if (m_mathObject.Success)
                {
                    // value like at Assets/Utils/Editor/BaseEditor.cs:28
                    if (m_mathObject.Value.Contains(TempLogManager.MANAGER_NAME))
                        continue;

                    filePath = m_mathObject.Value.Substring(0, m_mathObject.Value.IndexOf(':'));
                    tempLineStr = m_mathObject.Value.Substring(m_mathObject.Value.IndexOf(':') + 1);
                    filePath = filePath.Replace('/', '\\');
                    if (!Int32.TryParse(tempLineStr, out lineNum))
                        lineNum = 1;

                    break;
                }
            }

        }

    }
#endif

}