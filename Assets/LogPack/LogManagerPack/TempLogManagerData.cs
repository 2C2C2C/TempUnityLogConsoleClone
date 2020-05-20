using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CustomLog
{
    [CreateAssetMenu(menuName = "CustomLog/Log Manager Setting")]
    public class TempLogManagerData : SingletonScriptableObject<TempLogManagerData>
    {
        public bool m_showLog = true;
        public bool m_showWarning = true;
        public bool m_showError = true;

        private List<TempLogItem> m_tempLogs = null;

        public void SetTempData(bool[] showFlag, List<TempLogItem> tempLogs)
        {

        }

        public void GetTempData(out List<TempLogItem> tempLogs)
        {
            tempLogs = new List<TempLogItem>(m_tempLogs);
        }

    }
}