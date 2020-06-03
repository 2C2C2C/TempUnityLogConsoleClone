#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CustomLog
{
    [System.Serializable]
    public class TempLogManagerSettingPack
    {
        public bool IsShowLog = true;
        public bool IsShowWarning = true;
        public bool IsShowError = true;

        public bool IsClearOnPlay = true;
        public bool IsErrorPause = true;

        public bool WriteFileInEditor = true;

        public int[] m_categoryForUnShow = null;

        public TempLogManagerSettingPack()
        {
            m_categoryForUnShow = new int[0];
            IsShowLog = IsShowWarning = IsShowError = true;
            IsErrorPause = false;
            WriteFileInEditor = false;
        }

        public TempLogManagerSettingPack(TempLogManagerSettingPack pack)
        {
            IsShowLog = pack.IsShowLog;
            IsShowWarning = pack.IsShowWarning;
            IsShowError = pack.IsShowError;

            IsClearOnPlay = pack.IsClearOnPlay;
            IsErrorPause = pack.IsErrorPause;

            m_categoryForUnShow = new int[pack.m_categoryForUnShow.Length];
            Array.Copy(pack.m_categoryForUnShow, m_categoryForUnShow, pack.m_categoryForUnShow.Length);

            WriteFileInEditor = pack.WriteFileInEditor;
        }

        public string GetSettingPackString()
        {
            string result = "LogManager setting:\n";
            result += $"IsShowLog: {IsShowLog}\nIsShowWarning: {IsShowWarning}\nIsShowError: {IsShowError}\n";
            result += $"IsClearOnPlay: {IsClearOnPlay}\nIsErrorPause: {IsErrorPause}\nWriteFileInEditor: {WriteFileInEditor}\n";

            return result;
        }

    }
  
}
#endif