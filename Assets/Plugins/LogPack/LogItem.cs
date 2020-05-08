using UnityEngine;

namespace CustomLog
{
    public class LogItem
    {
        public bool IsSelected { get; set; }
        public readonly string LogItme = string.Empty;
        public readonly string LogMessage = string.Empty;
        public readonly string LogStackTrace = string.Empty;
        public readonly LogType GetLogType = LogType.Log;

        public LogItem(string message, string stackTrace, LogType type, bool isSelected = false)
        {
            IsSelected = isSelected;
            LogItme = System.DateTime.Now.ToString("HH:mm:ss");
            LogMessage = message;
            LogStackTrace = stackTrace;
            GetLogType = type;
        }
    }
}
