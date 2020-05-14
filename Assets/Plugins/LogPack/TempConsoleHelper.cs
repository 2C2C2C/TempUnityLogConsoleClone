#if UNITY_EDITOR

using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace CustomLog
{
    public static class TempConsoleHelper
    {
        private static string[] m_strings = null;
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

        private static string[] m_mutilineStackTrace = null;
        private static string m_tempStackTrace = null;

        // TODO : why shit PC OS has these 2 kinda of shit
        // to match "Assets\Plugins\LogPack\TempLogManager.cs(620,25)"
        public static readonly string REGEX_MATCH_PAT_1 = @"Assets\\.*cs\([0-9,]*\)";

        // to match "at Assets/Utils/Editor/BaseEditor.cs:28"
        public static readonly string REGEX_MATCH_PAT_2 = @"Assets/.*cs:[0-9]*";
        private static Match m_mathObject = null;
        public static void GetTopFileOfCallStack(in string callstack, out string filePath, out int lineNum)
        {
            filePath = string.Empty;
            lineNum = 0;
            m_tempStackTrace = string.Copy(callstack);

            m_mutilineStackTrace = callstack.Split('\n');

            string tempLineStr = null;

            for (int i = 0; i < m_mutilineStackTrace.Length; i++)
            {
                m_mathObject = Regex.Match(m_mutilineStackTrace[i], REGEX_MATCH_PAT_1, RegexOptions.IgnoreCase);

                if (m_mathObject.Success)
                {
                    // value like Assets\Plugins\LogPack\TempLogManager.cs(620,25)
                    if (m_mathObject.Value.Contains(TempLogManager.MANAGER_NAME))
                        continue;

                    filePath = m_mathObject.Value.Substring(0, m_mathObject.Value.IndexOf('('));
                    lineNum = m_mathObject.Value.IndexOf(',') - m_mathObject.Value.IndexOf('(') - 1;
                    tempLineStr = m_mathObject.Value.Substring(m_mathObject.Value.IndexOf('(') + 1, lineNum);
                    if (!Int32.TryParse(tempLineStr, out lineNum))
                        lineNum = 1;
                    break;
                }

                m_mathObject = Regex.Match(m_mutilineStackTrace[i], REGEX_MATCH_PAT_2, RegexOptions.IgnoreCase);
                if (m_mathObject.Success)
                {
                    // value like at Assets/Utils/Editor/BaseEditor.cs:28
                    if (m_mathObject.Value.Contains(TempLogManager.MANAGER_NAME))
                        continue;

                    filePath = m_mathObject.Value.Substring(0, m_mathObject.Value.IndexOf(':'));
                    tempLineStr = m_mathObject.Value.Substring(m_mathObject.Value.IndexOf(':') + 1);
                    filePath = filePath.Replace('/', '\\');
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

        public static bool TryGoTopCalledCode(in TempLogItem logClicked)
        {
            bool result = false;
            string stackTrace = string.IsNullOrEmpty(logClicked.LogStackTrace) ? logClicked.LogMessage : logClicked.LogStackTrace;
            GetTopFileOfCallStack(stackTrace, out string filePath, out int lineNum);
            result = UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(filePath, lineNum);
            return result;
        }

    }
}


#endif