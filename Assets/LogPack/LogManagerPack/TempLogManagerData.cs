#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CustomLog
{
    [System.Serializable]
    public class TempLogManagerSettingPack
    {
        public static readonly string TEMP_LOG_SETTING_FILEPATH = "";

        public bool IsShowLog { get; set; }
        public bool IsShowWarning { get; set; }
        public bool IsShowError { get; set; }
        public bool IsClearOnPlay { get; set; }
        public bool IsClearOnBuild { get; set; }
        public bool IsErrorPause { get; set; }
        public bool WriteFileInEditor { get; set; }

        private List<TempLogItem> m_tempLogs = null;

        public TempLogManagerSettingPack()
        {
            m_tempLogs = new List<TempLogItem>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="showFlag"> log,warn,error </param>
        /// <param name="tempLogs"> current logs </param>
        public void SetTempData(bool[] showFlag, List<TempLogItem> tempLogs)
        {
            // zzz
            IsShowLog = showFlag[0];
            IsShowWarning = showFlag[1];
            IsShowError = showFlag[2];

            // sure need clear all
            m_tempLogs.Clear();
            m_tempLogs.AddRange(tempLogs);
        }

        public void GetTempData(out List<TempLogItem> tempLogs)
        {
            tempLogs = new List<TempLogItem>(m_tempLogs);
        }
    }

    [CreateAssetMenu(menuName = "CustomLog/Log Manager Setting")]
    public class TempLogManagerData : SingletonScriptableObject<TempLogManagerData>
    {
        public TempLogManagerSettingPack m_dataPack = null;
    }
}
#endif