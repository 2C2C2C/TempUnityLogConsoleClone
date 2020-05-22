using System;
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

        private static StreamWriter _logFileWriter = null;
        private static readonly string LOG_FILE_NAME = "LogFile";

        private static string[] _logCategoryStrs = null;

#if UNITY_EDITOR

        #region variables for editor

        public static int PrevShowLogCount { get; private set; }
        public static int CurrentShowLogCount { get; private set; }

        public static readonly Type SelfType = typeof(TempLogManager);

        private static List<TempLogItem> _logItems = null;
        public static TempLogItem SelectedItem { get; set; }

        public static bool IsClearOnPlay = false;
        public static bool IsClearOnBuild = false;
        public static bool IsErrorPause = false;
        public static bool WriteFileInEditor = false;
        public static bool IsShowLog = true;
        public static bool IsShowWarning = true;
        public static bool IsShowError = true;

        public static event Action OnLogItemCreated;

        public static event Action OnLogsFreshed;

        private static readonly int REFRESH_INTERVAL = 20;
        private static readonly int REFRESH_ROLLBACK = 5;
        private static int _refreshTime = 0;
        private static bool _needFreshed = false;

        #region to get unity console and logs

        public static readonly int LOG_FLAG = 1 << 7;
        public static readonly int WARNING_FLAG = 1 << 8;
        public static readonly int ERROR_FLAG = 1 << 9;
        private static int _logFlags = 0;

        private static Type _entriesType = null;
        private static Type _entryType = null;
        private static Type _consoleWindow = null;

        private static Type UnityConsoleWindow
        {
            get
            {
                if (null == _consoleWindow)
                {
                    _consoleWindow = System.Type.GetType("UnityEditor.ConsoleWindow, UnityEditor.dll");
                }
                return _consoleWindow;
            }
        }

        private static Type UnityLogEntry
        {
            get
            {
                if (null == _entryType)
                {
                    _entryType = System.Type.GetType("UnityEditor.LogEntry, UnityEditor.dll");
                }
                return _entryType;
            }
        }

        private static Type UnityLogEntries
        {
            get
            {
                if (null == _entriesType)
                {
                    _entriesType = System.Type.GetType("UnityEditor.LogEntries,UnityEditor.dll");
                }
                return _entriesType;
            }
        }

        public static int UnityLogCount
        {
            get
            {
                return (int)UnityLogEntries.GetMethod("GetCount").Invoke(null, null);
            }
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

            _logFlags = LOG_FLAG | WARNING_FLAG | ERROR_FLAG;

            NormalLogCount = 0;
            WarningLogCount = 0;
            ErrorLogCount = 0;

#if UNITY_EDITOR
            InitLogmanagerForEditor();
            EditorApplication.update += OnEditorUpdate;
            WriteFileInEditor = false;
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

#if UNITY_EDITOR // here are some methods only used in editor

        public static void ClearLogs()
        {
            SelectedItem = null;
            NormalLogCount = 0;
            WarningLogCount = 0;
            ErrorLogCount = 0;
            _logItems.Clear();
            ClearUnityLogConsole();
            _needFreshed = true;
        }

        public static void GetLogs(out List<TempLogItem> logs)
        {
            PrevShowLogCount = CurrentShowLogCount;
            CurrentShowLogCount = 0;
            // too bad
            logs = new List<TempLogItem>();
            for (int i = 0; i < _logItems.Count; i++)
            {
                if (_logItems[i].GetLogTypeFlag == (_logItems[i].GetLogTypeFlag & _logFlags))
                {
                    CurrentShowLogCount++;
                    logs.Add(_logItems[i]);
                }
            }
        }

        public static void SetShowingLogFlag(bool enableLog, bool enableWarning, bool enableError)
        {
            _logFlags = 0;
            if (enableLog)
                _logFlags = _logFlags | LOG_FLAG;
            if (enableWarning)
                _logFlags = _logFlags | WARNING_FLAG;
            if (enableError)
                _logFlags = _logFlags | ERROR_FLAG;
        }

        private static void OnPlayModeChanged(PlayModeStateChange playMode)
        {
            if (PlayModeStateChange.EnteredPlayMode == playMode && IsClearOnPlay)
            {
                ClearLogs();
            }

            if (PlayModeStateChange.EnteredPlayMode == playMode && !WriteFileInEditor)
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
            _logItems = new List<TempLogItem>();

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

            int count = UnityLogCount;
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
                    _logItems.Add(logItem);
                }

            }
            var end = UnityLogEntries.GetMethod("EndGettingEntries", BindingFlags.Static | BindingFlags.Public);
            end.Invoke(null, null);

            setall.Invoke(null, new object[] { LOG_FLAG, true });
            setall.Invoke(null, new object[] { WARNING_FLAG, true });
            setall.Invoke(null, new object[] { ERROR_FLAG, true });
        }

        private static void OnEditorUpdate()
        {
            _refreshTime++;
            if (_refreshTime >= REFRESH_INTERVAL)
            {
                if (_needFreshed)
                {
                    _refreshTime = 0;
                    OnLogsFreshed?.Invoke();
                    _needFreshed = false;
                }
                else
                    _refreshTime -= REFRESH_ROLLBACK;
            }
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
                _logItems.Add(log);
                OnLogItemCreated?.Invoke();
                _needFreshed = true;
#endif
                WriteLogToFile(log);
            }
        }

        private static void FreshFileWriter()
        {
            if (null != _logFileWriter)
                CloseFileWriter();

            string path = null;
            path = $"{Application.persistentDataPath}/{LOG_FILE_NAME}_{System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}.txt";
            _logFileWriter = new StreamWriter(File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite), System.Text.UTF8Encoding.Default);
            _logFileWriter.WriteLine($"Game launch");
            _logFileWriter.WriteLine($"Time : {System.DateTime.Now.ToString()}");
            _logFileWriter.WriteLine($"----------------------------------------\n");
        }

        private static void CloseFileWriter()
        {
            if (null != _logFileWriter)
            {
                _logFileWriter.WriteLine($"Game End at {System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}");
                _logFileWriter.WriteLine($"Result:\n log : {NormalLogCount}\n warning : {WarningLogCount}\n error : {ErrorLogCount}\n");
                _logFileWriter.Dispose();
                _logFileWriter.Close();
            }
            _logFileWriter = null;
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
                if (_logFileWriter == null)
                {
                    FreshFileWriter();
                }

                // write log to file
                _logFileWriter.WriteLine($"{logItem.GetLogType}\n{logItem.LogItme}\n{logItem.LogMessage}\n\n{logItem.LogStackTrace}");
                _logFileWriter.WriteLine("-----------------------------\n");
            }
        }

        private static void OnGameQuit()
        {
            Application.logMessageReceived -= LogMessageReceived;
            Application.quitting -= OnGameQuit;
            CloseFileWriter();
        }

    }
}

