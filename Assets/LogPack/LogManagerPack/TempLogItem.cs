using UnityEngine;

namespace CustomLog
{
    public class TempLogItem
    {
        public bool IsSelected { get; set; }
        public readonly string LogTime = string.Empty;
        public readonly string LogMessage = string.Empty;
        public readonly string LogStackTrace = string.Empty;
        public readonly LogType LogType = LogType.Log;
        public readonly int LogTypeFlag = 0;
        // TODO : add this later
        // public readonly UnityEngine.Object ContextObject = null;


        public TempLogItem(string message, string stackTrace, LogType type, int logFlag, bool isSelected = false)
        {
            IsSelected = isSelected;
            LogTime = System.DateTime.Now.ToString("HH:mm:ss");
            LogMessage = message;
            LogStackTrace = stackTrace;
            LogType = type;
            LogTypeFlag = logFlag;
        }
    }
}
