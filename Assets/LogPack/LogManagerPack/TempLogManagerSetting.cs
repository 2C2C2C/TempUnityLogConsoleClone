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
        public bool IsClearOnPlay;
        public bool IsErrorPause;

        public bool WriteFileInEditor;
        public int LogTypeFlag;
        public float UpperPanelSizeRatio;

        public bool HasValue;

        public TempLogManagerSettingPack(int logTypeFlag)
        {
            LogTypeFlag = logTypeFlag;
            IsClearOnPlay = false;
            IsErrorPause = false;
            WriteFileInEditor = false;
            HasValue = true;
            UpperPanelSizeRatio = 0.5f;
        }

        public TempLogManagerSettingPack(in TempLogManagerSettingPack pack)
        {
            if (pack.HasValue)
            {
                LogTypeFlag = pack.LogTypeFlag;
                IsClearOnPlay = pack.IsClearOnPlay;
                IsErrorPause = pack.IsErrorPause;
                WriteFileInEditor = pack.WriteFileInEditor;
                UpperPanelSizeRatio = pack.UpperPanelSizeRatio;
            }
            else
            {
                IsClearOnPlay = false;
                IsErrorPause = false;
                LogTypeFlag = 1 << 7 | 1 << 8 | 1 << 9;
                WriteFileInEditor = false;
                UpperPanelSizeRatio = 0.5f;
            }

            HasValue = true;
        }

        public string GetSettingPackString()
        {
            string result = "LogManager setting:\n";
            result += $"ShowLogFlag: {LogTypeFlag}\n";
            result += $"IsClearOnPlay: {IsClearOnPlay}\nIsErrorPause: {IsErrorPause}\nWriteFileInEditor: {WriteFileInEditor}\n";

            return result;
        }

    }

}
#endif