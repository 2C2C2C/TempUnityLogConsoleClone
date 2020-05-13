using UnityEngine;

namespace CustomLog
{
    public class TempLogItem
    {
        public bool IsSelected { get; set; }
        public readonly string LogItme = string.Empty;
        public readonly string LogMessage = string.Empty;
        public readonly string LogStackTrace = string.Empty;
        public readonly LogType GetLogType = LogType.Log;
        public readonly int GetLogTypeFlag = 0;
        // TODO : add this later
        // public readonly UnityEngine.Object ContextObject = null;


        public TempLogItem(string message, string stackTrace, LogType type, int logFlag, bool isSelected = false)
        {
            IsSelected = isSelected;
            LogItme = System.DateTime.Now.ToString("HH:mm:ss");
            LogMessage = message;
            LogStackTrace = stackTrace;
            GetLogType = type;
            GetLogTypeFlag = logFlag;
        }
    }
}
