#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace CustomLog
{
    public static class TempLogManagerForUnityEditor
    {
        public static int InfoLogCount { get; private set; }
        public static int WarningLogCount { get; private set; }
        public static int ErrorLogCount { get; private set; }

        private static List<TempLogItem> _logItems = null;
        private static TempLogItem _selectedItem = null;

        private static bool _isCustomLogWindowEnabled = false;
        public static bool IsLogCustomWindowEnabled => _isCustomLogWindowEnabled;

        private static bool _isClearOnPlay = false;
        public static bool IsClearOnPlay
        {
            get => _isClearOnPlay;

            set
            {
                _needSave = _needSave || (value != _isClearOnPlay);
                _isClearOnPlay = value;
            }
        }

        private static bool _isShowLog = true;
        public static bool IsShowLog
        {
            get => _isShowLog;
            set
            {
                bool changed = (value != _isShowLog);
                if (changed)
                {
                    _needRefresh = true;
                    _needSave = true;
                    ShowLogTypeFlag = value ? (ShowLogTypeFlag | LOG_FLAG) : (ShowLogTypeFlag ^ LOG_FLAG);
                }
                _isShowLog = value;
            }
        }
        private static bool _isShowWarning = true;
        public static bool IsShowWarning
        {
            get => _isShowWarning;
            set
            {
                bool changed = (value != _isShowWarning);
                if (changed)
                {
                    _needRefresh = true;
                    _needSave = true;
                    ShowLogTypeFlag = value ? (ShowLogTypeFlag | WARNING_FLAG) : (ShowLogTypeFlag ^ WARNING_FLAG);
                }
                _isShowWarning = value;
            }
        }
        private static bool _isShowError = true;
        public static bool IsShowError
        {
            get => _isShowError;
            set
            {
                bool changed = (value != _isShowError);
                if (changed)
                {
                    _needRefresh = true;
                    _needSave = true;
                    ShowLogTypeFlag = value ? (ShowLogTypeFlag | ERROR_FLAG) : (ShowLogTypeFlag ^ ERROR_FLAG);
                }
                _isShowError = value;
            }
        }
        public static int ShowLogTypeFlag { get; set; }
        public static float UpperSizeRatio { get; set; }

        private static TempLogManagerSettingPack _currentPack = default;
        public static TempLogManagerSettingPack GetInitPack() => _currentPack;

        private static bool _writeLogFileInEditor = false;

        private static readonly int LOG_UPDATE_INTERVAL = 15;

        private static int _updateTime = 0;
        private static bool _needRefresh = false;
        private static bool _isCompiling = false;

        private static bool _needSave = false;
        private static int _autoSaveTimer = 0;
        private static readonly int SAVE_INTERVAL = 200;
        private static bool _hasInit = false;

        public static event Action OnLogsUpdated;

        #region to get unity console and logs

        private static readonly int LOG_FLAG = 1 << 7;
        private static readonly int WARNING_FLAG = 1 << 8;
        private static readonly int ERROR_FLAG = 1 << 9;
        private static readonly int CLEAR_ON_PLAY_FLAG = 1 << 1;

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

            PropertyInfo flagProperty = null;
            flagProperty = UnityLogEntries.GetProperty("consoleFlags");

            object consoleFlagObj = flagProperty.GetValue(null);
            // to capture the logs already in Unity Console
            InfoLogCount = GetLogsFromUnityConsole(LogType.Log);
            WarningLogCount = GetLogsFromUnityConsole(LogType.Warning);
            ErrorLogCount = GetLogsFromUnityConsole(LogType.Error);

            // reset the log flag as user want
            var setall = UnityConsoleWindow.GetMethod("SetFlag", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            setall.Invoke(null, new object[] { LOG_FLAG, IsShowLog });
            setall.Invoke(null, new object[] { WARNING_FLAG, IsShowWarning });
            setall.Invoke(null, new object[] { ERROR_FLAG, IsShowError });

            flagProperty.SetValue(null, consoleFlagObj);

            //Debug.Log($"loaded logs {m_logItems.Count}");
            //Debug.Log($"log count:\ninfo {InfoLogCount}\nwarninvg {WarningLogCount}\nerror {ErrorLogCount}");
        }

        #endregion

        [InitializeOnLoadMethod]
        public static void InitLogManager()
        {
            if (_hasInit)
                return;

            InfoLogCount = WarningLogCount = ErrorLogCount = 0;
            TempLogManagerHelper.LoadLogManagerSettingFile(out _currentPack);
            // Debug.Log("load editor log manager setting");
            ApplySettingPack(in _currentPack);

            TempLogManager.InitLogManager();
            TempLogManager.SetFlagOFWriteFile(_writeLogFileInEditor);
            _isCustomLogWindowEnabled = (null != TempConsoleWindow.Instance);

            GetLogsOfromUnityConsole();
            TempConsoleWindow.OnTempConsoleCreated += OnTempConsoleCreated;
            TempConsoleWindow.OnTempConsoleDestroyed += OnTempConsoleDestroyed;
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
            _hasInit = true;
        }

        public static void SetSelectedItem(TempLogItem nextSelectedItem)
        {
            _selectedItem = nextSelectedItem;
        }

        public static void GetLogs(ref List<TempLogItem> logs)
        {
            // TODO : do not new a list everytime, cache 1
            logs.Clear();
            for (int i = 0; i < _logItems.Count; i++)
            {
                if ((ShowLogTypeFlag & _logItems[i].LogTypeFlag) != 0)
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
            if (IsLogCustomWindowEnabled)
                ClearUnityLogConsole();
            _needRefresh = true;
        }

        public static void ApplySettingPack(in TempLogManagerSettingPack pack)
        {
            ShowLogTypeFlag = pack.LogTypeFlag;
            IsClearOnPlay = pack.IsClearOnPlay;
            _writeLogFileInEditor = pack.WriteFileInEditor;
            UpperSizeRatio = pack.UpperPanelSizeRatio;
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

        public static void LoadSetting()
        {
            TempLogManagerHelper.LoadLogManagerSettingFile(out _currentPack);
            ApplySettingPack(in _currentPack);
        }

        public static void SaveSetting()
        {
            _currentPack.IsClearOnPlay = IsClearOnPlay;
            _currentPack.WriteFileInEditor = _writeLogFileInEditor;
            _currentPack.UpperPanelSizeRatio = UpperSizeRatio;
            _currentPack.LogTypeFlag = ShowLogTypeFlag;

            TempLogManagerHelper.SaveLogManagerSettingFile(_currentPack);
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

        private static void OnTempConsoleDestroyed()
        {
            _isCustomLogWindowEnabled = false;
            SaveSetting();
        }

        private static void OnTempConsoleCreated()
        {
            _isCustomLogWindowEnabled = true;
        }

        private static void OnEditorQuitting()
        {
            SaveSetting();
            TempConsoleWindow.OnTempConsoleDestroyed -= SaveSetting;
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