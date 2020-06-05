using System;
using System.IO;
using UnityEngine;
using CustomLog;
using UnityEditor.Callbacks;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class TempLogManager
{
    private static readonly int LOG_FLAG = 1 << 7;
    private static readonly int WARNING_FLAG = 1 << 8;
    private static readonly int ERROR_FLAG = 1 << 9;
    private static bool _hasInited = false;

    private static int _normalLogCount = 0;
    private static int _warningLogCount = 0;
    private static int _errorLogCount = 0;

    public static readonly string MANAGER_TAG = "[LogManager]";
    private static StreamWriter _logFileWriter = null;
    private static readonly string LOG_FILE_NAME = "LogFile";
    private static bool _writeLogFile = true;

#if UNITY_EDITOR
    public static event Action<TempLogItem> OnLogItemCreated;
#endif

    public static void InitLogManager()
    {
        _hasInited = false;
        Application.logMessageReceived -= LogMessageReceived;
        Application.logMessageReceived += LogMessageReceived;
        Application.quitting -= OnGameQuit;
        Application.quitting += OnGameQuit;

#if UNITY_STANDALONE
        if (m_writeLogFile)
            FreshFileWriter();
#endif
        _normalLogCount = 0;
        _warningLogCount = 0;
        _errorLogCount = 0;

        // Debug.Log("LogManager Inited");
        _hasInited = true;
    }

    #region log methods

    public static void Log(in string message)
    {
        CreateLog(message, LogType.Log);
    }

    public static void Log(in object obj)
    {
        CreateLog(obj.ToString(), LogType.Log);
    }

    public static void Log(string format, params object[] args)
    {
        CreateLog(string.Format(format, args), LogType.Log);
    }

    public static void LogWarning(in string message)
    {
        CreateLog(message, LogType.Warning);
    }

    public static void LogWarning(in object obj)
    {
        CreateLog(obj.ToString(), LogType.Warning);
    }

    public static void LogWarning(string format, params object[] args)
    {
        CreateLog(string.Format(format, args), LogType.Warning);
    }

    public static void LogError(in string message)
    {
        CreateLog(message, LogType.Error);
    }

    public static void LogError(in object obj)
    {
        CreateLog(obj.ToString(), LogType.Error);
    }

    public static void LogError(string format, params object[] args)
    {
        CreateLog(string.Format(format, args), LogType.Error);
    }

    #endregion

    #region methods for editor

#if UNITY_EDITOR
    // here are some methods only used in editor
    [OnOpenAsset(1)]
    public static bool LocateRealFile(int instanceID, int line)
    {
        // if someone still using default console, it is till fine
        string stackTrace = TempLogManagerHelper.GetStackTrace();
        string[] traceLines = stackTrace.Split('\n');
        string filePath = String.Empty;
        int lineNum = 0;
        bool result = false;

        for (int i = 0; i < traceLines.Length; i++)
        {
            TempLogManagerHelper.TryGetFilePathFromStr(traceLines[i], out filePath, out lineNum);
            if (string.IsNullOrEmpty(filePath) || filePath.Contains(TempLogManagerHelper.LOGMANAGER_FILE_NAME))
            {
                continue;
            }
            else
            {
                filePath = filePath.Replace('/', '\\');
                result = UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(filePath, lineNum);
                break;
            }
        }

        return result;
    }

    public static void StartWriteLogFile()
    {
        if (Application.isPlaying)
        {
            FreshFileWriter();
        }
    }

    public static void EndWriteLogFile()
    {
        CloseFileWriter();
    }

#endif

    #endregion

    public static void SetFlagOFWriteFile(bool value)
    {
        _writeLogFile = value;
        if (!_writeLogFile)
        {
            CloseFileWriter();
        }
        else if (_writeLogFile && Application.isPlaying)
        {
            FreshFileWriter();
        }
    }

    private static int GetLogFlagByType(LogType logType)
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

    private static void CreateLog(in string message, LogType logType)
    {
        if (!_hasInited)
            InitLogManager();

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

    private static void LogMessageReceived(string message, string stackTrace, LogType type)
    {
        TempLogItem log = null;
        switch (type)
        {
            case LogType.Error:
                _errorLogCount++;
                break;
            case LogType.Assert:
                type = LogType.Error;
                _errorLogCount++;
                break;
            case LogType.Warning:
                _warningLogCount++;
                break;
            case LogType.Log:
                _normalLogCount++;
                break;
            case LogType.Exception:
                type = LogType.Error;
                _errorLogCount++;
                break;
            default:
                type = LogType.Error;
                _errorLogCount++;
                break;
        }

        if (-1 < message.IndexOf(MANAGER_TAG))
        {
            try
            {
                // it's our custom log, add it
                string logCateStr = message;
                logCateStr = logCateStr.Remove(0, MANAGER_TAG.Length);
                logCateStr = logCateStr.Substring(logCateStr.IndexOf('[') + 1, logCateStr.IndexOf(']') - logCateStr.IndexOf('[') - 1);
                log = new TempLogItem(message, stackTrace, type, 0);
            }
            catch (Exception e) // 
            {
                Debug.LogError($"LogManager got a error with custom log {e.InnerException}");
                log = new TempLogItem(message, stackTrace, type, GetLogFlagByType(type));
            }
        }
        else
        {
            // maybe it's default log, add it
            if (string.IsNullOrEmpty(stackTrace))
                TempLogManagerHelper.SplitUnityLog(ref message, out stackTrace, type);

            log = new TempLogItem(message, stackTrace, type, GetLogFlagByType(type));
        }

        if (null != log)
        {
#if UNITY_EDITOR
            OnLogItemCreated?.Invoke(log);
#endif
            WriteLogToFile(log);
        }
    }

    private static void FreshFileWriter()
    {
        if (null != _logFileWriter)
        {
            CloseFileWriter();
        }

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
            _logFileWriter.WriteLine($"Result:\n log : {_normalLogCount}\n warning : {_warningLogCount}\n error : {_errorLogCount}\n");
            _logFileWriter.Dispose();
            _logFileWriter.Close();
        }
        _logFileWriter = null;
    }

    private static void WriteLogToFile(in TempLogItem logItem)
    {
#if UNITY_EDITOR
        if (!_writeLogFile || (Application.isEditor && !EditorApplication.isPlaying))
            return;
#endif

        if (Application.isPlaying && _writeLogFile)
        {
            if (_logFileWriter == null)
                FreshFileWriter();

            // write log to file
            _logFileWriter.WriteLine($"{logItem.LogType}\n{logItem.LogTime}\n{logItem.LogMessage}\n\n{logItem.LogStackTrace}");
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