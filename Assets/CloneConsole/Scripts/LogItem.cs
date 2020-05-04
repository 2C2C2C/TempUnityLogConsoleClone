using UnityEngine;

namespace TempConsole
{
    public class LogItem
    {
        public bool IsSelected { get; set; }
        public readonly string LogInfo = string.Empty;
        public readonly string LogMessage = string.Empty;
        public readonly LogType GetLogType = LogType.Log;

        public LogItem(bool isSelected, string info, string message, LogType type)
        {
            IsSelected = isSelected;
            LogInfo = string.Format("[{0}] {1}", System.DateTime.Now.ToLongTimeString(), info);
            LogMessage = message;
            GetLogType = type;
        }

    }
    
}