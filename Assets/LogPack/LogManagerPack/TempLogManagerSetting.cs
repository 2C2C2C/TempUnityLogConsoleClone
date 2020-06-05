#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CustomLog
{
    [System.Serializable]
    public struct TempLogManagerSettingPack
    {
        public bool IsShowLog;
        public bool IsShowWarning;
        public bool IsShowError;

        public bool IsClearOnPlay;
        public bool IsErrorPause;

        public bool WriteFileInEditor;

        public int[] m_categoryForUnShow;

        public TempLogManagerSettingPack(int[] catesUnshow = null)
        {
            if (null == catesUnshow)
                m_categoryForUnShow = new int[0];
            else
            {
                m_categoryForUnShow = new int[catesUnshow.Length];
                Array.Copy(catesUnshow, m_categoryForUnShow, catesUnshow.Length);
            }
            IsShowLog = IsShowWarning = IsShowError = true;
            IsClearOnPlay = false;
            IsErrorPause = false;
            WriteFileInEditor = false;
        }

        public TempLogManagerSettingPack(in TempLogManagerSettingPack pack)
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