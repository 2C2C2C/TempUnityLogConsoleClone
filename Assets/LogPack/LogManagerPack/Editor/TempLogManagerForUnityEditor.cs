#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace CustomLog
{
    public static class LogManagerForUnityEditor
    {
        public static int InfoLogCount { get; private set; }
        public static int WarningLogCount { get; private set; }
        public static int ErrorLogCount { get; private set; }

        private static List<TempLogItem> _logItems = null;
        private static TempLogItem _selectedItem = null;

        private static bool _isClearOnPlay = false;
        public static bool IsClearOnPlay
        {
            get => _isClearOnPlay;

            set
            {
                _needRefresh = _needRefresh || (value != _isClearOnPlay);
                _isClearOnPlay = value;
            }
        }

        // private static bool _isErrorPause = false;
        // public static bool IsErrorPause
        // {
        //     get => _isErrorPause;

        //     set
        //     {
        //         _needRefresh = _needRefresh || (value != _isErrorPause);
        //         _isErrorPause = value;
        //     }
        // }

        private static bool _isShowLog = true;
        public static bool IsShowLog
        {
            get => _isShowLog;
            set
            {
                _needRefresh = _needRefresh || (value != _isShowLog);
                _isShowLog = value;
            }
        }
        private static bool _isShowWarning = true;
        public static bool IsShowWarning
        {
            get => _isShowWarning;
            set
            {
                _needRefresh = _needRefresh || (value != _isShowWarning);
                _isShowWarning = value;
            }
        }
        private static bool _isShowError = true;
        public static bool IsShowError
        {
            get => _isShowError;
            set
            {
                _needRefresh = _needRefresh || (value != _isShowError);
                _isShowError = value;
            }
        }

        private static TempLogManagerSettingPack _currentPack = default;
        public static TempLogManagerSettingPack GetInitPack() => _currentPack;

        private static bool _writeLogFileInEditor = false;

        private static readonly int LOG_UPDATE_INTERVAL = 15;
        // private static readonly int LOG_UPDATE_ROLLBACK = 5;
        private static int _updateTime = 0;
        private static bool _needRefresh = false;
        private static bool _isCompiling = false;

        private static bool _needSave = false;
        private static int _autoSaveTimer = 0;
        private static readonly int SAVE_INTERVAL = 500;


        public static event Action OnLogsUpdated;

        #region to get unity console and logs

        static readonly int LOG_FLAG = 1 << 7;
        static readonly int WARNING_FLAG = 1 << 8;
        static readonly int ERROR_FLAG = 1 << 9;

        static Type m_entriesType = null;
        static Type m_entryType = null;
        static Type m_consoleWindow = null;

        static Type UnityConsoleWindow
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

        static Type UnityLogEntry
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

        static Type UnityLogEntries
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
        public static void InitLogManager()
        {
            InfoLogCount = WarningLogCount = ErrorLogCount = 0;
            TempLogManagerHelper.LoadLogManagerSettingFile(out var settingPack);
            // Debug.Log("load editor log manager setting");
            _currentPack = new TempLogManagerSettingPack(settingPack);
            ApplySettingPack(in settingPack);

            TempLogManager.InitLogManager();
            GetLogsOfromUnityConsole();
            TempConsoleWindow.OnTempConsoleClosed += SaveSetting;
            AssemblyReloadEvents.beforeAssemblyReload += SaveSetting;

            _updateTime = 0;
            _needRefresh = false;

            TempLogManager.OnLogItemCreated -= AddNewLogItem;
            TempLogManager.OnLogItemCreated += AddNewLogItem;
            TempLogManager.InitLogManager();

            EditorApplication.update -= EditorTick;
            EditorApplication.update += EditorTick;
            EditorApplication.quitting -= OnEditorQuitting;
            EditorApplication.quitting += OnEditorQuitting;
            // Debug.Log("Editor LogManager Inited");
        }

        public static void SetSelectedItem(TempLogItem nextSelectedItem)
        {
            _selectedItem = nextSelectedItem;
        }

        public static void GetLogs(ref List<TempLogItem> logs)
        {
            // TODO : do not new a list everytime, cache 1
            logs.Clear();

            int flag = 0;
            if (IsShowLog)
                flag = flag | LOG_FLAG;
            if (IsShowWarning)
                flag = flag | WARNING_FLAG;
            if (IsShowError)
                flag = flag | ERROR_FLAG;

            for (int i = 0; i < _logItems.Count; i++)
            {
                if ((flag & _logItems[i].LogTypeFlag) != 0)
                    logs.Add(_logItems[i]);
            }
        }

        public static void ClearLogs()
        {
            _selectedItem = null;
            InfoLogCount = 0;
            WarningLogCount = 0;
            ErrorLogCount = 0;
            _logItems.Clear();
            ClearUnityLogConsole();
            _needRefresh = true;
        }

        public static void ApplySettingPack(in TempLogManagerSettingPack pack)
        {
            IsShowLog = pack.IsShowLog;
            IsShowWarning = pack.IsShowWarning;
            IsShowError = pack.IsShowError;
            IsClearOnPlay = pack.IsClearOnPlay;
            _writeLogFileInEditor = pack.WriteFileInEditor;
        }

        public static void AddNewLogItem(TempLogItem log)
        {
            switch (log.LogType)
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
                    InfoLogCount++;
                    break;
                case LogType.Exception:
                    ErrorLogCount++;
                    break;
                default:
                    break;
            }
            _needRefresh = true;
            _logItems.Add(log);
        }

        public static void SetWriteFileFlag(bool value)
        {
            if (_writeLogFileInEditor == value)
                return;

            _writeLogFileInEditor = value;
            if (Application.isPlaying)
                TempLogManager.SetFlagOFWriteFile(_writeLogFileInEditor);

        }

        public static int GetLogsFromUnityConsole(LogType logType)
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

            string message = string.Empty;
            string stackTrace = string.Empty;
            int count = GetUnityLogCount();
            TempLogItem logItem = null;
            for (int i = 0; i < count; i++)
            {
                if (i >= startIndex)
                {
                    var e = Activator.CreateInstance(UnityLogEntry);
                    m.Invoke(null, new object[] { i, e });

                    FieldInfo field = e.GetType().GetField("condition", BindingFlags.Instance | BindingFlags.Public);
                    message = (string)field.GetValue(e);

                    TempLogManagerHelper.SplitUnityLog(ref message, out stackTrace);
                    logItem = new TempLogItem(message, stackTrace, logType, enableLogFlag);
                    _logItems.Add(logItem);
                }

            }
            var end = UnityLogEntries.GetMethod("EndGettingEntries", BindingFlags.Static | BindingFlags.Public);
            end.Invoke(null, null);

            setall.Invoke(null, new object[] { LOG_FLAG, true });
            setall.Invoke(null, new object[] { WARNING_FLAG, true });
            setall.Invoke(null, new object[] { ERROR_FLAG, true });
            return count;
        }

        private static void GetLogsOfromUnityConsole()
        {
            _logItems = new List<TempLogItem>();

            // to capture the logs already in Unity Console
            InfoLogCount = GetLogsFromUnityConsole(LogType.Log);
            WarningLogCount = GetLogsFromUnityConsole(LogType.Warning);
            ErrorLogCount = GetLogsFromUnityConsole(LogType.Error);

            // reset the log flag as user want
            var setall = UnityConsoleWindow.GetMethod("SetFlag", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            setall.Invoke(null, new object[] { LOG_FLAG, IsShowLog });
            setall.Invoke(null, new object[] { WARNING_FLAG, IsShowWarning });
            setall.Invoke(null, new object[] { ERROR_FLAG, IsShowError });

            //Debug.Log($"loaded logs {m_logItems.Count}");
            //Debug.Log($"log count:\ninfo {InfoLogCount}\nwarninvg {WarningLogCount}\nerror {ErrorLogCount}");
        }

        private static void OnCodeCompileStart(object obj)
        {
            if (!_isCompiling)
            {
                SaveSetting();
                _isCompiling = true;
            }
        }

        private static void OnPlayModeChanged(PlayModeStateChange playMode)
        {
            if (PlayModeStateChange.EnteredPlayMode == playMode && IsClearOnPlay)
                ClearLogs();

            if (PlayModeStateChange.EnteredPlayMode == playMode && _writeLogFileInEditor)
                TempLogManager.StartWriteLogFile();

            if (PlayModeStateChange.ExitingPlayMode == playMode)
                TempLogManager.EndWriteLogFile();
        }

        private static void SaveSetting()
        {
            _currentPack.IsClearOnPlay = IsClearOnPlay;
            _currentPack.IsShowLog = IsShowLog;
            _currentPack.IsShowWarning = IsShowWarning;
            _currentPack.IsShowError = IsShowError;
            _currentPack.WriteFileInEditor = _writeLogFileInEditor;

            TempLogManagerHelper.SaveLogManagerSettingFile(_currentPack);
        }

        private static void OnEditorQuitting()
        {
            SaveSetting();
            TempConsoleWindow.OnTempConsoleClosed -= SaveSetting;
            AssemblyReloadEvents.beforeAssemblyReload -= SaveSetting;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.quitting -= OnEditorQuitting;
        }

        private static void EditorTick()
        {
            if (_needRefresh && ++_updateTime >= LOG_UPDATE_INTERVAL)
            {
                // if get new logs
                // update new logs, notify console
                _updateTime = 0;
                OnLogsUpdated?.Invoke();
                _needRefresh = false;
            }

            if (_needSave && ++_autoSaveTimer >= SAVE_INTERVAL)
            {
                SaveSetting();
                _autoSaveTimer = 0;
                _needSave = false;
            }

        }

    }
}
#endif