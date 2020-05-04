﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace TempConsole
{
    public class TempConsoleWindow : EditorWindow
    {
        private static UnityEngine.Object _lock = new UnityEngine.Object();
        private static TempConsoleWindow _instance = null;
        public static TempConsoleWindow Instance
        {
            get
            {
                lock (_lock)
                {
                    if (null == _instance)
                    {
                        _instance = FindObjectOfType<TempConsoleWindow>();
                    }
                }

                return _instance;
            }
        }

        private Rect m_menuUpperBar = default;
        private Rect m_upperPanel = default;
        private Rect m_lowerPanel = default;
        private Rect m_resizer = default;

        private float MENU_BAR_HEIGHT = 20.0f;
        private float m_upperSizeRatio = 0.5f;
        private readonly float RESIZER_HEIGHT = 4.0f;
        private float PanelGroupHeight => position.height - MENU_BAR_HEIGHT;
        private bool m_isResizing = false;

        // private bool m_isCollapse = false;
        private bool m_isClearOnPlay = false;
        private bool m_isClearOnBuild = false;
        public bool IsClearOnBuild => m_isClearOnBuild;
        private bool m_isErrorPause = false;
        private bool m_isShowLog = true;
        private bool m_isShowWarning = true;
        private bool m_isShowError = true;

        private Vector2 m_upperPanelScroll = default;
        private Vector2 m_lowerPanelScroll = default;

        private GUIStyle m_panelLabelStyle = default;
        private GUIStyle m_panelStyle = default;
        private GUIStyle m_resizerStyle = default;
        private GUIStyle m_boxItemStyle = default;
        private GUIStyle m_textAreaStyle = default;
        private GUIStyle m_labelButtonStyle = default;

        private List<LogItem> m_logItems = null;
        private HashSet<LogType> m_logTypeForUnshow = null;
        private LogItem m_selectedLogItem = null;

        private int m_normalLogCount = 0;
        private int m_warningLogCount = 0;
        private int m_errorLogCount = 0;
        private List<string> m_numStrs = null;

        #region icons

        private Texture2D m_infoIcon = null;
        private Texture2D m_infoIconSmall = null;
        private Texture2D m_warningIcon = null;
        private Texture2D m_warningIconSmall = null;
        private Texture2D m_errorIcon = null;
        private Texture2D m_errorIconSmall = null;

        private Texture2D m_boxBgOdd = null;
        private Texture2D m_boxBgEven = null;
        private Texture2D m_boxBgSelected = null;
        private Texture2D m_boxIcon = null;

        #endregion

        public void JumpToCurrentLogPos()
        {
            if (null == m_selectedLogItem)
            {
                return;
            }

            if (!TempConsoleHelper.TryGoToCode(in m_selectedLogItem))
            {
                Debug.LogError("Temp Console Error : code jump error");
            }
        }

        public void ClearLogs()
        {
            if (null != m_selectedLogItem)
            {
                m_selectedLogItem.IsSelected = false;
            }
            m_selectedLogItem = null;

            m_normalLogCount = 0;
            m_warningLogCount = 0;
            m_errorLogCount = 0;
            m_logItems.Clear();
            GUI.changed = true;
        }

        [MenuItem("Window/Temp Console")]
        private static void OpenWindow()
        {
            TempConsoleWindow window = GetWindow<TempConsoleWindow>();
            GUIContent titleContent = new GUIContent("TempConsole", EditorGUIUtility.Load("icons/UnityEditor.ConsoleWindow.png") as Texture2D, "a clone sonsole");
            window.titleContent = titleContent;
        }

        #region draw methods

        private void DrawMenuUpperBar()
        {
            m_menuUpperBar = new Rect(0.0f, 0.0f, this.position.width, MENU_BAR_HEIGHT);

            // draw upper bar, for default console stuff
            GUILayout.BeginArea(m_menuUpperBar, EditorStyles.toolbar);
            GUILayout.BeginHorizontal();

            if (GUILayout.Button(new GUIContent("Clear"), EditorStyles.toolbarButton, GUILayout.Width(40.0f)))
            {
                ClearLogs();
            }
            GUILayout.Space(5.0f);

            // m_isCollapse = GUILayout.Toggle(m_isCollapse, new GUIContent("Collapse"), EditorStyles.toolbarButton, GUILayout.Width(55.0f));
            m_isClearOnPlay = GUILayout.Toggle(m_isClearOnPlay, new GUIContent("Clear On Play"), EditorStyles.toolbarButton, GUILayout.Width(80.0f));
            m_isClearOnBuild = GUILayout.Toggle(m_isClearOnBuild, new GUIContent("Clear On Build"), EditorStyles.toolbarButton, GUILayout.Width(85.0f));
            m_isErrorPause = GUILayout.Toggle(m_isErrorPause, new GUIContent("Error Pause"), EditorStyles.toolbarButton, GUILayout.Width(70.0f));

            GUILayout.FlexibleSpace();

            m_normalLogCount = Mathf.Clamp(m_normalLogCount, 0, 100);
            m_warningLogCount = Mathf.Clamp(m_warningLogCount, 0, 100);
            m_errorLogCount = Mathf.Clamp(m_errorLogCount, 0, 100);
            m_isShowLog = GUILayout.Toggle(m_isShowLog, new GUIContent(m_numStrs[m_normalLogCount], m_infoIconSmall), EditorStyles.toolbarButton, GUILayout.Width(30.0f));
            m_isShowWarning = GUILayout.Toggle(m_isShowWarning, new GUIContent(m_numStrs[m_warningLogCount], m_warningIconSmall), EditorStyles.toolbarButton, GUILayout.Width(30.0f));
            m_isShowError = GUILayout.Toggle(m_isShowError, new GUIContent(m_numStrs[m_errorLogCount], m_errorIconSmall), EditorStyles.toolbarButton, GUILayout.Width(30.0f));

            m_logTypeForUnshow.Clear();
            if (!m_isShowLog)
            {
                m_logTypeForUnshow.Add(LogType.Log);
            }

            if (!m_isShowWarning)
            {
                m_logTypeForUnshow.Add(LogType.Warning);
            }

            if (!m_isShowError)
            {
                m_logTypeForUnshow.Add(LogType.Error);
                m_logTypeForUnshow.Add(LogType.Assert);
                m_logTypeForUnshow.Add(LogType.Exception);
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawUpperPanel()
        {
            m_upperPanel = new Rect(0, MENU_BAR_HEIGHT, this.position.width, (this.position.height - MENU_BAR_HEIGHT) * m_upperSizeRatio);
            GUILayout.BeginArea(m_upperPanel, m_panelStyle);
            GUILayout.Label("Log", m_panelLabelStyle);

            // draw all logs
            m_upperPanelScroll = GUILayout.BeginScrollView(m_upperPanelScroll);
            for (int i = 0; i < m_logItems.Count; i++)
            {
                if (m_logTypeForUnshow.Contains(m_logItems[i].GetLogType))
                {
                    continue;
                }

                if (DrawLogBox(m_logItems[i].LogInfo, m_logItems[i].GetLogType, i % 2 == 0, m_logItems[i].IsSelected))
                {
                    if (null != m_selectedLogItem)
                    {
                        if (m_logItems[i] == m_selectedLogItem)
                        {
                            // click a some one, open code
                            JumpToCurrentLogPos();

                        }
                        else
                        {
                            m_selectedLogItem.IsSelected = false;
                            m_selectedLogItem = m_logItems[i];
                            m_selectedLogItem.IsSelected = true;
                        }
                    }
                    else
                    {
                        m_selectedLogItem = m_logItems[i];
                        m_selectedLogItem.IsSelected = true;
                    }
                    GUI.changed = true;
                }
            }
            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        private void DrawLowerPanel()
        {
            float yPos = PanelGroupHeight * m_upperSizeRatio + MENU_BAR_HEIGHT + RESIZER_HEIGHT;
            m_lowerPanel = new Rect(0, yPos, this.position.width, PanelGroupHeight * (1.0f - m_upperSizeRatio));
            GUILayout.BeginArea(m_lowerPanel, m_panelStyle);
            GUILayout.Label("Log Detail", m_panelLabelStyle);

            m_lowerPanelScroll = GUILayout.BeginScrollView(m_lowerPanelScroll);

            string logDetail = null;
            string[] logDetailMutiLine = null;

            // TODO : code clean here
            string pathline = "";
            string tempCase = ".cs:";
            string path = string.Empty;
            int line = 0;
            int splitwa = 0;

            if (null != m_selectedLogItem)
            {
                logDetail = m_selectedLogItem.LogMessage;
                GUILayout.TextArea(string.Format("{0}\n", m_selectedLogItem.LogInfo), m_textAreaStyle);

                logDetailMutiLine = logDetail.Split('\n');
                for (int i = 0; i < logDetailMutiLine.Length; i++)
                {
                    // Regex.Match 'at xxx'
                    Match matches = Regex.Match(logDetailMutiLine[i], @"\(at (.+)\)", RegexOptions.Multiline);

                    if (matches.Success)
                    {
                        while (matches.Success)
                        {
                            pathline = matches.Groups[1].Value;
                            if (pathline.Contains(tempCase))
                            {
                                int splitIndex = pathline.LastIndexOf(":");
                                path = pathline.Substring(0, splitIndex);
                                line = Convert.ToInt32(pathline.Substring(splitIndex + 1));
                                string fullpath = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf("Assets"));
                                fullpath = fullpath + path;
                                splitwa = logDetailMutiLine[i].LastIndexOf("(");
                                logDetailMutiLine[i] = logDetailMutiLine[i].Substring(0, splitwa);

                                GUILayout.BeginHorizontal();
                                GUILayout.TextArea(string.Format(" (at : {0})\n", logDetailMutiLine[i]), m_textAreaStyle);
                                if (GUILayout.Button(string.Format(" ( {0} )\n", pathline), m_labelButtonStyle))
                                {
                                    UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(fullpath.Replace('/', '\\'), line);
                                }
                                GUILayout.FlexibleSpace();
                                GUILayout.EndHorizontal();
                                break;
                            }
                        }
                    }
                    else
                    {
                        GUILayout.TextArea(logDetailMutiLine[i], m_textAreaStyle);
                    }

                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawResizer()
        {
            float yPos = (this.position.height - MENU_BAR_HEIGHT) * m_upperSizeRatio + MENU_BAR_HEIGHT;
            m_resizer = new Rect(0, yPos, this.position.width, RESIZER_HEIGHT);

            GUILayout.BeginArea(new Rect(m_resizer.position + (Vector2.up * RESIZER_HEIGHT), new Vector2(this.position.width, 2.0f)), m_resizerStyle);
            GUILayout.EndArea();

            EditorGUIUtility.AddCursorRect(m_resizer, MouseCursor.ResizeVertical);
        }

        private bool DrawLogBox(in string content, LogType logType, bool isOdd, bool isSelected)
        {
            if (isSelected)
            {
                m_boxItemStyle.normal.background = m_boxBgSelected;
            }
            else
            {
                if (isOdd)
                {
                    m_boxItemStyle.normal.background = m_boxBgOdd;
                }
                else
                {
                    m_boxItemStyle.normal.background = m_boxBgEven;
                }
            }

            switch (logType)
            {
                case LogType.Error:
                    m_boxIcon = m_errorIcon;
                    break;
                case LogType.Assert:
                    m_boxIcon = m_errorIcon;
                    break;
                case LogType.Warning:
                    m_boxIcon = m_warningIcon;
                    break;
                case LogType.Log:
                    m_boxIcon = m_infoIcon;
                    break;
                case LogType.Exception:
                    m_boxIcon = m_errorIcon;
                    break;
                default:
                    break;
            }

            return GUILayout.Button(new GUIContent(content, m_boxIcon), m_boxItemStyle, GUILayout.ExpandWidth(true), GUILayout.Height(30.0f));
        }

        #endregion draw methods

        private void ProcessEvents(Event currentEvent)
        {
            if (EventType.MouseDown == currentEvent.type)
            {
                // if press mouse left in resizer
                m_isResizing = (0 == currentEvent.button && m_resizer.Contains(currentEvent.mousePosition));
            }
            else if (EventType.MouseUp == currentEvent.type)
            {
                m_isResizing = false;
            }

            Resize(currentEvent);
        }

        private void Resize(Event currentEvent)
        {
            if (m_isResizing)
            {
                float pos = currentEvent.mousePosition.y - MENU_BAR_HEIGHT;

                m_upperSizeRatio = pos / PanelGroupHeight;
                m_upperSizeRatio = Mathf.Clamp(m_upperSizeRatio, 0.5f, 0.8f);
                //Debug.Log($"next upper ratio {m_upperSizeRatio}");
                Repaint();
            }
        }

        private void GetAssets()
        {
            m_panelLabelStyle = new GUIStyle();
            m_panelLabelStyle.fixedHeight = 30.0f;
            m_panelLabelStyle.richText = true;
            m_panelLabelStyle.normal.textColor = Color.white;
            m_panelLabelStyle.fontSize = 20;

            m_infoIcon = EditorGUIUtility.Load("icons/console.infoicon.png") as Texture2D;
            m_infoIconSmall = EditorGUIUtility.Load("icons/console.infoicon.sml.png") as Texture2D;
            m_warningIcon = EditorGUIUtility.Load("icons/console.warnicon.png") as Texture2D;
            m_warningIconSmall = EditorGUIUtility.Load("icons/console.warnicon.sml.png") as Texture2D;
            m_errorIcon = EditorGUIUtility.Load("icons/console.erroricon.png") as Texture2D;
            m_errorIconSmall = EditorGUIUtility.Load("icons/console.erroricon.sml.png") as Texture2D;

            m_resizerStyle = new GUIStyle();

            m_panelStyle = new GUIStyle();
            m_panelStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/projectbrowsericonareabg.png") as Texture2D;
            // m_panelStyle.normal.background = GUI.skin.window.normal.background; // IDK why this doesnt work....

            m_boxItemStyle = new GUIStyle();
            m_boxItemStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);

            m_boxBgOdd = EditorGUIUtility.Load("builtin skins/darkskin/images/cn entrybackodd.png") as Texture2D;
            m_boxBgEven = EditorGUIUtility.Load("builtin skins/darkskin/images/cnentrybackeven.png") as Texture2D;
            m_boxBgSelected = EditorGUIUtility.Load("builtin skins/darkskin/images/menuitemhover.png") as Texture2D;

            m_textAreaStyle = new GUIStyle();
            m_textAreaStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
            m_textAreaStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/projectbrowsericonareabg.png") as Texture2D;

            m_labelButtonStyle = new GUIStyle();
            m_labelButtonStyle.normal.textColor = Color.green;
            m_labelButtonStyle.normal.background = m_textAreaStyle.normal.background;
            m_labelButtonStyle.alignment = TextAnchor.MiddleLeft;
            m_labelButtonStyle.stretchWidth = false;
            var b = m_labelButtonStyle.border;
            b.left = 0;
            b.right = 0;
            b.top = 0;
            b.bottom = 0;
            m_labelButtonStyle.border = b;

        }

        private void InitData()
        {
            m_upperSizeRatio = 0.5f;
            m_logItems = new List<LogItem>();
            m_selectedLogItem = null;

            m_numStrs = new List<string>();
            for (int i = 0; i < 100; i++)
            {
                m_numStrs.Add(i.ToString());
            }
            m_numStrs.Add("99+"); // I dun wanna deal with too much shit

            m_logTypeForUnshow = new HashSet<LogType>();

            m_errorLogCount =
            m_warningLogCount =
            m_normalLogCount = 0;
        }

        private void LogMessageReceived(string condition, string stackTrace, LogType type)
        {
            LogItem log = new LogItem(false, condition, stackTrace, type);
            m_logItems.Add(log);
            switch (type)
            {
                case LogType.Error:
                    m_errorLogCount++;
                    break;
                case LogType.Assert:
                    m_errorLogCount++;
                    break;
                case LogType.Warning:
                    m_warningLogCount++;
                    break;
                case LogType.Log:
                    m_normalLogCount++;
                    break;
                case LogType.Exception:
                    m_errorLogCount++;
                    break;
                default:
                    m_errorLogCount++;
                    break;
            }

            Repaint();
            //GUI.changed = true;
        }

        private void ReceiveTempLog(LogItem logItem)
        {
            m_logItems.Add(logItem);
            switch (logItem.GetLogType)
            {
                case LogType.Error:
                    m_errorLogCount++;
                    break;
                case LogType.Assert:
                    m_errorLogCount++;
                    break;
                case LogType.Warning:
                    m_warningLogCount++;
                    break;
                case LogType.Log:
                    m_normalLogCount++;
                    break;
                case LogType.Exception:
                    m_errorLogCount++;
                    break;
                default:
                    m_errorLogCount++;
                    break;
            }

            Repaint();
            //GUI.changed = true;
        }

        private void OnPlayModeChanged(PlayModeStateChange nextPlayMode)
        {
            switch (nextPlayMode)
            {
                case PlayModeStateChange.EnteredEditMode:
                    break;
                case PlayModeStateChange.ExitingEditMode:
                    break;
                case PlayModeStateChange.EnteredPlayMode:
                    if (m_isClearOnPlay)
                    {
                        ClearLogs();
                    }
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    break;
                default:
                    break;
            }
        }

        private void OnEnable()
        {
            GetAssets();

            m_isShowLog = true;
            m_isShowWarning = true;
            m_isShowError = true;

            // bind events
            Application.logMessageReceived += LogMessageReceived;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;

            InitData();
        }

        private void OnGUI()
        {
            DrawMenuUpperBar();
            DrawUpperPanel();
            DrawLowerPanel();
            DrawResizer();

            ProcessEvents(Event.current);
            if (GUI.changed)
            {
                Repaint();
            }
        }

        private void OnDestroy()
        {
            // should be save to unbind here
            Application.logMessageReceived -= LogMessageReceived;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;

            if (this == TempConsoleWindow.Instance)
                TempConsoleWindow._instance = null;
        }

    }

    public static class TempConsoleHelper
    {
        public static bool TryGoToCode(in LogItem logClicked)
        {
            bool result = false;
            string stackTrace = logClicked.LogMessage;

            // try to find the top call
            if (!string.IsNullOrEmpty(stackTrace))
            {
                // regular expression check 'at xxx'
                Match matches = Regex.Match(stackTrace, @"\(at (.+)\)", RegexOptions.IgnoreCase);
                string pathline = "";
                int splitIndex = 0;
                int line = 0;
                if (matches.Success)
                {
                    pathline = matches.Groups[1].Value;
                    splitIndex = pathline.LastIndexOf(":");
                    string path = pathline.Substring(0, splitIndex);
                    line = Convert.ToInt32(pathline.Substring(splitIndex + 1));
                    path = $"{Application.dataPath.Substring(0, Application.dataPath.LastIndexOf("Assets"))}{path}";
                    result = UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(path.Replace('/', '\\'), line);
                }
            }
            return result;
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnCodeCompiled()
        {
            // shoud
            if (TempConsoleWindow.Instance && TempConsoleWindow.Instance.IsClearOnBuild)
                TempConsoleWindow.Instance.ClearLogs();
        }

    }

}