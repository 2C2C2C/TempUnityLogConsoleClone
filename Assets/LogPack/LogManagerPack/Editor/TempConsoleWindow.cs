using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace CustomLog
{
    public class TempConsoleWindow : EditorWindow
    {
        private static readonly string WindowName = "Temp Console Window";

        private bool m_hasInited = false;
        private bool m_needRefresh = false;
        private int m_prevCount = 0;
        private int m_currentShowCount = 0;
        private Rect m_menuUpperBar = default;
        private Rect m_upperPanel = default;
        private Rect m_lowerPanel = default;
        private Rect m_resizer = default;

        private readonly float MENU_BAR_HEIGHT = 20.0f;
        private readonly float LOG_FLAG_SIZE = 50.0f;
        private float m_upperSizeRatio = 0.5f;
        private readonly float RESIZER_HEIGHT = 4.0f;
        private float GetPanelGroupHeight() => position.height - MENU_BAR_HEIGHT;
        private bool m_isResizing = false;

        private bool m_isClearOnPlay = false;
        // private bool m_isErrorPause = false;
        private bool m_writeFileInEditorMode = false;
        private bool m_isShowLog = true;
        private bool m_isShowWarning = true;
        private bool m_isShowError = true;
        private bool m_isAutoScroll = true;

        private Vector2 m_upperPanelScroll = default;
        private Vector2 m_lowerPanelScroll = default;

        private GUIStyle m_panelStyle = default;
        private GUIStyle m_resizerStyle = default;
        private GUIStyle m_boxIconStyle = default;
        private GUIStyle m_boxItemStyle = default;
        private GUIStyle m_textAreaStyle = default;
        private GUIStyle m_labelButtonStyle = default;

        private List<TempLogItem> m_currentShowingItems = null;
        private TempLogItem m_selectedLogItem = null;
        private HashSet<LogType> m_logTypeForUnshow = null;

        private int m_normalLogCount = 0;
        private int m_warningLogCount = 0;
        private int m_errorLogCount = 0;

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
        private Texture m_boxIcon = null;

        #endregion

        #region events

        public static System.Action OnTempConsoleClosed;

        #endregion

        public void JumpToStackTop()
        {
            if (null == m_selectedLogItem)
            {
                return;
            }

            if (Application.isEditor)
            {
                TempLogManagerHelper.TryGoToTopOfStack(m_selectedLogItem);
            }
        }

        public void ClearLogs()
        {
            m_selectedLogItem = null;
            LogManagerForUnityEditor.ClearLogs();
        }

        [MenuItem("Window/Temp Log Window")]
        static void OpenWindow()
        {
            TempConsoleWindow window = GetWindow<TempConsoleWindow>();
            Texture2D icon = EditorGUIUtility.Load("icons/UnityEditor.ConsoleWindow.png") as Texture2D;
            window.titleContent = new GUIContent(WindowName, icon);
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

            m_isClearOnPlay = GUILayout.Toggle(LogManagerForUnityEditor.IsClearOnPlay, new GUIContent("Clear On Play"), EditorStyles.toolbarButton, GUILayout.Width(80.0f));
            LogManagerForUnityEditor.IsClearOnPlay = m_isClearOnPlay;
            // m_isErrorPause = GUILayout.Toggle(LogManagerForUnityEditor.IsErrorPause, new GUIContent("Error Pause"), EditorStyles.toolbarButton, GUILayout.Width(70.0f));
            // LogManagerForUnityEditor.IsErrorPause = m_isErrorPause;

            m_writeFileInEditorMode = GUILayout.Toggle(m_writeFileInEditorMode, new GUIContent("Write Log File"), EditorStyles.toolbarButton, GUILayout.Width(120.0f));
            LogManagerForUnityEditor.SetWriteFileFlag(m_writeFileInEditorMode);

            GUILayout.FlexibleSpace();

            m_normalLogCount = Mathf.Clamp(LogManagerForUnityEditor.InfoLogCount, 0, 100);
            m_warningLogCount = Mathf.Clamp(LogManagerForUnityEditor.WarningLogCount, 0, 100);
            m_errorLogCount = Mathf.Clamp(LogManagerForUnityEditor.ErrorLogCount, 0, 100);

            m_isShowLog = GUILayout.Toggle(LogManagerForUnityEditor.IsShowLog, new GUIContent(TempLogManagerHelper.GetNumberStr(m_normalLogCount), m_infoIconSmall), EditorStyles.toolbarButton, GUILayout.Width(LOG_FLAG_SIZE));
            m_isShowWarning = GUILayout.Toggle(LogManagerForUnityEditor.IsShowWarning, new GUIContent(TempLogManagerHelper.GetNumberStr(m_warningLogCount), m_warningIconSmall), EditorStyles.toolbarButton, GUILayout.Width(LOG_FLAG_SIZE));
            m_isShowError = GUILayout.Toggle(LogManagerForUnityEditor.IsShowError, new GUIContent(TempLogManagerHelper.GetNumberStr(m_errorLogCount), m_errorIconSmall), EditorStyles.toolbarButton, GUILayout.Width(LOG_FLAG_SIZE));
            LogManagerForUnityEditor.IsShowLog = m_isShowLog;
            LogManagerForUnityEditor.IsShowWarning = m_isShowWarning;
            LogManagerForUnityEditor.IsShowError = m_isShowError;

            int prevCount = m_logTypeForUnshow.Count;
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

            m_needRefresh = m_needRefresh || (prevCount != m_logTypeForUnshow.Count);

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawUpperPanel()
        {
            m_upperPanel = new Rect(0, MENU_BAR_HEIGHT, this.position.width, (this.position.height - MENU_BAR_HEIGHT) * m_upperSizeRatio);
            GUILayout.BeginArea(m_upperPanel, m_panelStyle);
            // re-adjust scroller position
            if (m_prevCount < m_currentShowCount)
            {
                if (m_isAutoScroll)
                {
                    m_upperPanelScroll.y = (m_currentShowCount * m_boxItemStyle.fixedHeight) - m_upperPanel.height;
                    m_isAutoScroll = false;
                }
                else
                {
                    float tempGap = (m_prevCount * m_boxItemStyle.fixedHeight) - m_upperPanel.height;
                    if (tempGap > 0)
                    {
                        if (tempGap - m_upperPanelScroll.y <= m_boxItemStyle.fixedHeight || m_isAutoScroll)
                        {
                            m_upperPanelScroll.y = (m_currentShowCount * m_boxItemStyle.fixedHeight) - m_upperPanel.height;
                        }
                    }
                }
            }
            else if (m_currentShowCount * m_boxItemStyle.fixedHeight < m_upperPanel.height)
            {
                m_isAutoScroll = true;
            }

            m_prevCount = m_currentShowCount;
            m_upperPanelScroll = GUILayout.BeginScrollView(m_upperPanelScroll);

            // draw items
            for (int i = 0; i < m_currentShowingItems.Count; i++)
            {
                if (DrawLogBox(m_currentShowingItems[i], i % 2 == 0, m_currentShowingItems[i].IsSelected))
                {
                    if (null != m_selectedLogItem)
                    {
                        if (m_currentShowingItems[i] == m_selectedLogItem)
                        {
                            // click a some one, open code
                            JumpToStackTop();
                        }
                        else
                        {
                            m_selectedLogItem.IsSelected = false;
                            m_selectedLogItem = m_currentShowingItems[i];
                            LogManagerForUnityEditor.SetSelectedItem(m_selectedLogItem);
                            m_selectedLogItem.IsSelected = true;
                        }
                    }
                    else
                    {
                        m_selectedLogItem = m_currentShowingItems[i];
                        LogManagerForUnityEditor.SetSelectedItem(m_selectedLogItem);
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
            float yPos = GetPanelGroupHeight() * m_upperSizeRatio + MENU_BAR_HEIGHT + RESIZER_HEIGHT;
            m_lowerPanel = new Rect(0, yPos, this.position.width, GetPanelGroupHeight() * (1.0f - m_upperSizeRatio));
            GUILayout.BeginArea(m_lowerPanel, m_panelStyle);

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
                logDetail = m_selectedLogItem.LogStackTrace;
                GUILayout.TextArea(string.Format("{0}\n", m_selectedLogItem.LogMessage, m_textAreaStyle));

                logDetailMutiLine = logDetail.Split('\n');
                for (int i = 0; i < logDetailMutiLine.Length; i++)
                {
                    Match matches = Regex.Match(logDetailMutiLine[i], @"\(at .*\.cs:[0-9]*\)", RegexOptions.Multiline);

                    if (matches.Success)
                    {
                        while (matches.Success)
                        {
                            pathline = matches.Value;
                            if (pathline.Contains(tempCase))
                            {
                                // TODO : CLEAN HERE!!!
                                int splitIndex = pathline.LastIndexOf(":");
                                path = pathline.Substring(0, splitIndex);
                                line = Convert.ToInt32(pathline.Substring(splitIndex + 1, pathline.Length - splitIndex - 2));
                                string fullpath = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf("Assets"));
                                // HACK : get rid of 'at '
                                fullpath = fullpath + path.Substring(path.IndexOf(" ") + 1);
                                splitwa = logDetailMutiLine[i].LastIndexOf("(");
                                logDetailMutiLine[i] = logDetailMutiLine[i].Substring(0, splitwa);

                                GUILayout.BeginHorizontal();
                                GUILayout.TextArea(string.Format(" (at : {0})\n", logDetailMutiLine[i]), m_textAreaStyle);
                                if (GUILayout.Button(string.Format("{0}\n", pathline), m_labelButtonStyle))
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

        private bool DrawLogBox(in TempLogItem logItem, bool isOdd, bool isSelected)
        {
            if (isSelected)
            {
                m_boxItemStyle.normal.background = m_boxBgSelected;
                m_boxIconStyle.normal.background = m_boxBgSelected;
            }
            else
            {
                if (isOdd)
                {
                    m_boxItemStyle.normal.background = m_boxBgOdd;
                    m_boxIconStyle.normal.background = m_boxBgOdd;
                }
                else
                {
                    m_boxItemStyle.normal.background = m_boxBgEven;
                    m_boxIconStyle.normal.background = m_boxBgEven;
                }
            }

            switch (logItem.LogType)
            {
                case LogType.Error:
                    m_boxIcon = m_errorIcon;
                    break;
                case LogType.Assert:
                    m_boxIcon = m_errorIcon;
                    break;
                case LogType.Exception:
                    m_boxIcon = m_errorIcon;
                    break;
                case LogType.Warning:
                    m_boxIcon = m_warningIcon;
                    break;
                case LogType.Log:
                    m_boxIcon = m_infoIcon;
                    break;

                default:
                    break;
            }

            bool result1 = true;
            bool result2 = true;

            GUILayout.BeginHorizontal();
            //GUILayoutUtility.GetRect(new GUIContent(m_boxIcon), m_boxIconStyle);
            //GUITools.PopupLayout
            //Rect boxIconRect = new Rect(, m_boxIconStyle.CalcSize); 
            result1 = GUILayout.Button(new GUIContent(m_boxIcon), m_boxIconStyle);
            result2 = GUILayout.Button(new GUIContent($"[{ logItem.LogTime}] {logItem.LogMessage}"), m_boxItemStyle, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();

            return result1 || result2;
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
                m_upperSizeRatio = pos / GetPanelGroupHeight();
                m_upperSizeRatio = Mathf.Clamp(m_upperSizeRatio, 0.5f, 0.8f);
                Repaint();
            }
        }

        private void GetAssets()
        {
            m_infoIcon = EditorGUIUtility.Load("icons/console.infoicon.png") as Texture2D;
            m_infoIconSmall = EditorGUIUtility.Load("icons/console.infoicon.sml.png") as Texture2D;
            m_warningIcon = EditorGUIUtility.Load("icons/console.warnicon.png") as Texture2D;
            m_warningIconSmall = EditorGUIUtility.Load("icons/console.warnicon.sml.png") as Texture2D;
            m_errorIcon = EditorGUIUtility.Load("icons/console.erroricon.png") as Texture2D;
            m_errorIconSmall = EditorGUIUtility.Load("icons/console.erroricon.sml.png") as Texture2D;

            m_resizerStyle = new GUIStyle();

            m_panelStyle = new GUIStyle();
            m_panelStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/projectbrowsericonareabg.png") as Texture2D;
            //m_panelStyle.normal.background = GUI.skin.window.normal.background;

            m_boxIconStyle = new GUIStyle();
            m_boxIconStyle.fixedHeight = 28.0f;
            m_boxIconStyle.fixedWidth = 40.0f;

            m_boxItemStyle = new GUIStyle();
            m_boxItemStyle.clipping = TextClipping.Clip;
            m_boxItemStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f); // ?
            m_boxItemStyle.fixedHeight = 28.0f;

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

        private void GetData()
        {
            // make it works for now
            LogManagerForUnityEditor.GetLogs(ref m_currentShowingItems);
            m_normalLogCount = LogManagerForUnityEditor.InfoLogCount;
            m_warningLogCount = LogManagerForUnityEditor.WarningLogCount;
            m_errorLogCount = LogManagerForUnityEditor.ErrorLogCount;

            m_currentShowCount = m_currentShowingItems.Count;
        }

        private void WannaRepaint()
        {
            GetData();
            Repaint();
        }

        private void ContainerInit()
        {
            // set log show flag

            m_isShowLog = LogManagerForUnityEditor.IsShowLog;
            m_isShowWarning = LogManagerForUnityEditor.IsShowWarning;
            m_isShowError = LogManagerForUnityEditor.IsShowError;
            m_logTypeForUnshow = new HashSet<LogType>();
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

            m_selectedLogItem = null;
            m_prevCount = m_currentShowCount = 0;
            m_currentShowingItems = new List<TempLogItem>();

            m_hasInited = true;
        }

        private void InitSomeStuff()
        {
            m_isClearOnPlay = LogManagerForUnityEditor.IsClearOnPlay;
            m_isShowLog = LogManagerForUnityEditor.IsShowLog;
            m_isShowWarning = LogManagerForUnityEditor.IsShowWarning;
            m_isShowError = LogManagerForUnityEditor.IsShowError;
        }

        #region life circle

        private void Awake()
        {
            m_upperSizeRatio = 0.5f;
        }

        private void OnEnable()
        {
            m_hasInited = false;
            GetAssets();
            ContainerInit();

            InitSomeStuff();
            LogManagerForUnityEditor.OnLogsUpdated += WannaRepaint;
            WannaRepaint();
        }

        private void OnGUI()
        {
            if (!m_hasInited)
                return;

            if (m_needRefresh)
            {
                GetData();
                m_needRefresh = false;
            }

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

        private void OnDisable()
        {
            m_currentShowingItems.Clear();
            LogManagerForUnityEditor.OnLogsUpdated -= WannaRepaint;
        }

        private void OnDestroy()
        {
            OnTempConsoleClosed?.Invoke();
        }

        #endregion

    }

}
