#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace CustomLog
{
    public static class TempLogManagerHelper
    {
        public static readonly string MANAGER_TAG = "[LogManager]";
        public static readonly string LOGMANAGER_NAME = "LogManager";
        public static readonly string LOGMANAGER_FILE_NAME = "LogManager.cs";

        static string[] m_strings = null;
        public static void GetNumberStr(int number, out string resultStr)
        {
            if (null == m_strings)
            {
                m_strings = new string[101];
                int i = 0;
                while (i < 100)
                {
                    m_strings[i] = i.ToString();
                    i++;
                }
                m_strings[i] = "99+";
            }

            resultStr = m_strings[Mathf.Clamp(number, 0, 100)];
        }

        public static string GetNumberStr(int number)
        {
            string resultStr = string.Empty;
            if (null == m_strings)
            {
                m_strings = new string[101];
                int i = 0;
                while (i < 100)
                {
                    m_strings[i] = i.ToString();
                    i++;
                }
                m_strings[i] = "99+";
            }

            resultStr = m_strings[Mathf.Clamp(number, 0, 100)];
            return resultStr;
        }

        public static bool TryGoToTopOfStack(in TempLogItem logClicked)
        {
            bool result = false;
            string stackTrace = null;
            stackTrace = string.IsNullOrEmpty(logClicked.LogStackTrace) ? logClicked.LogMessage : logClicked.LogStackTrace;
            GetTopFileOfCallStack(stackTrace, out string filePath, out int line);

            if (!string.IsNullOrEmpty(filePath))
            {
                result = UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(filePath, line);
            }

            return result;
        }

        // to match "Assets\Script\Combat\SkillAction.cs(620,25)"
        static readonly string REGEX_MATCH_PAT_1 = @"Assets\\.*cs\([0-9,]*\)";

        // to match "at Assets/Script/Combat/SkillAction.cs:28"
        static readonly string REGEX_MATCH_PAT_2 = @"Assets/.*cs:[0-9]*";
        private static Match _mathObject = null;

        private static string[] _mutilineStackTrace = null;
        private static string _tempStackTrace = null;

        public static void GetTopFileOfCallStack(in string callstack, out string filePath, out int lineNum)
        {
            filePath = string.Empty;
            lineNum = 0;
            _tempStackTrace = string.Copy(callstack);
            _mutilineStackTrace = callstack.Split('\n');
            string tempLineStr = null;

            for (int i = 0; i < _mutilineStackTrace.Length; i++)
            {
                _mathObject = Regex.Match(_mutilineStackTrace[i], REGEX_MATCH_PAT_1, RegexOptions.IgnoreCase);

                if (_mathObject.Success)
                {
                    // value like Assets\Script\Combat\SkillAction.cs(620,25)
                    if (_mathObject.Value.Contains(LOGMANAGER_NAME))
                        continue;

                    filePath = _mathObject.Value.Substring(0, _mathObject.Value.IndexOf('('));
                    lineNum = _mathObject.Value.IndexOf(',') - _mathObject.Value.IndexOf('(') - 1;
                    tempLineStr = _mathObject.Value.Substring(_mathObject.Value.IndexOf('(') + 1, lineNum);
                    if (!Int32.TryParse(tempLineStr, out lineNum))
                        lineNum = 1;
                    break;
                }

                _mathObject = Regex.Match(_mutilineStackTrace[i], REGEX_MATCH_PAT_2, RegexOptions.IgnoreCase);
                if (_mathObject.Success)
                {
                    // value like at at Assets/Script/Combat/SkillAction.cs:28
                    if (_mathObject.Value.Contains(LOGMANAGER_NAME))
                        continue;

                    filePath = _mathObject.Value.Substring(0, _mathObject.Value.IndexOf(':'));
                    tempLineStr = _mathObject.Value.Substring(_mathObject.Value.IndexOf(':') + 1);
                    if (!Int32.TryParse(tempLineStr, out lineNum))
                        lineNum = 1;

                    break;
                }
            }

            if (!string.IsNullOrEmpty(filePath))
            {
                filePath = string.Format("{0}{1}", Application.dataPath.Substring(0, Application.dataPath.LastIndexOf("Assets")), filePath);
                filePath = filePath.Replace('/', '\\');
            }

        }

        static readonly string REGEX_MATCH_PAT_3 = @"\(at .*\.cs:[0-9]*\)";
        public static bool TryGetFilePathFromStr(in string message, out string filePath, out int lineNum)
        {
            bool result = false;
            string tempStr = message;
            filePath = null;
            lineNum = 0;

            Match matche = Regex.Match(message, REGEX_MATCH_PAT_3, RegexOptions.IgnoreCase);
            result = matche.Success;
            if (result)
            {
                // HACK : get rid of "(at )"
                tempStr = matche.Value.Substring(4, matche.Value.Length - 5);
                int splitIndex = tempStr.LastIndexOf(":");
                filePath = tempStr.Substring(0, splitIndex);
                lineNum = Convert.ToInt32(tempStr.Substring(splitIndex + 1));
                filePath = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf("Assets")) + filePath;
            }

            return result;
        }

        static readonly string SPLIT_PAT = @"(at";
        public static void SplitUnityLog(ref string message, out string stackTrace, LogType logType = LogType.Log)
        {
            int split = 0;
            split = message.IndexOf(SPLIT_PAT);

            if (split > 0)
            {
                for (int i = split; i > -1; i--)
                {
                    if ('\n' == message[i])
                    {
                        // new cut here
                        split = i;
                        break;
                    }
                }
                stackTrace = message.Substring(split + 1);
                message = message.Substring(0, split);
            }
            else
            {
                // maybe it is a compile log
                stackTrace = string.Empty;
            }
        }

        private static readonly Type _unityGOType = typeof(GameObject);
        public static bool TryToLocateContext(UnityEngine.Object context)
        {
            bool result = false;

            if (null == context)
            {
                return result;
            }

            if (context.GetType() == _unityGOType)
            {
                //???
                // is GO
                GameObject go = context as GameObject;
                if (null == go.scene)
                {
                    Debug.Log("context go is a prefab");
                }
                else
                {
                    Debug.Log("context go is in scene now");
                }
            }


            return result;
        }
        public static readonly string EDITOR_LOG_SETTING_KEY = "LogManagerEditorSetting";

        public static void SaveLogManagerSettingFile(in TempLogManagerSettingPack pack)
        {
            // TODO : to create a TempLogManagerData
            //Application.persistentDataPath
            string jsonStr = JsonUtility.ToJson(pack);
            UnityEditor.EditorPrefs.SetString(EDITOR_LOG_SETTING_KEY, jsonStr);
            Debug.Log($"save log manager setting: \n{jsonStr}");
        }

        public static void LoadLogManagerSettingFile(out TempLogManagerSettingPack pack)
        {
            if (!UnityEditor.EditorPrefs.HasKey(EDITOR_LOG_SETTING_KEY))
            {
                pack = new TempLogManagerSettingPack();
            }
            else
            {
                string jsonStr = UnityEditor.EditorPrefs.GetString(EDITOR_LOG_SETTING_KEY);
                pack = JsonUtility.FromJson<TempLogManagerSettingPack>(jsonStr);
                //Debug.Log($"wanna load log manager setting: \n{jsonStr}");
            }

        }

        public static string GetStackTrace()
        {
            var consoleWindowType = typeof(EditorWindow).Assembly.GetType("UnityEditor.ConsoleWindow");
            var fieldInfo = consoleWindowType.GetField("ms_ConsoleWindow", BindingFlags.Static | BindingFlags.NonPublic);
            var consoleWindowInstance = fieldInfo.GetValue(null);

            if (null != consoleWindowInstance)
            {
                if ((object)EditorWindow.focusedWindow == consoleWindowInstance)
                {
                    fieldInfo = consoleWindowType.GetField("m_ActiveText", BindingFlags.Instance | BindingFlags.NonPublic);
                    string activeText = fieldInfo.GetValue(consoleWindowInstance).ToString();
                    return activeText;
                }
            }
            return "";
        }

    }
}
#endif
