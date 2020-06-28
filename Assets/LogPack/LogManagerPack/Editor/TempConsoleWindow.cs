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
        private static TempConsoleWindow _instance = null;
        public static TempConsoleWindow Instance => _instance;

        private bool m_hasInited = false;
        private bool m_needRefresh = false;
        private int m_prevCount = 0;
        private int m_currentShowCount = 0;
        private Rect m_menuUpperBar = default;
        private Rect m_upperPanel = default;
        private Rect m_lowerPanel = default;
        private Rect m_resizer = default;

        private const float MENU_BAR_HEIGHT = 20.0f;
        private const float LOG_FLAG_SIZE = 50.0f;
        private float m_upperSizeRatio = 0.5f;
        private readonly float RESIZER_HEIGHT = 4.0f;
        private float GetPanelGroupHeight() => position.height - MENU_BAR_HEIGHT;
        private const float LOG_ITEM_HEIGHT = 28.0f;
        private const float LOG_ITEM_ICON_WIDTH = 40.0f;
        private float m_scrollPosition = 0.0f;
        private bool m_isResizing = false;

        private bool m_isClearOnPlay = false;
        private bool m_writeFileInEditorMode = false;
        private bool m_isShowLog = true;
        private bool m_isShowWarning = true;
        private bool m_isShowError = true;
        private bool m_isAutoScroll = true;

        // private Vector2 m_upperPanelScroll = default;
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

        public static System.Action OnTempConsoleCreated;
        public static System.Action OnTempConsoleDestroyed;

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
            TempLogManagerForUnityEditor.ClearLogs();
        }

        [MenuItem("Window/Temp Log Window")]
        private static void OpenWindow()
        {
            TempConsoleWindow window = GetWindow<TempConsoleWindow>();
            Texture2D icon = EditorGUIUtility.Load("icons/UnityEditor.ConsoleWindow.png") as Texture2D;
            window.titleContent = new GUIContent(WindowName, icon);
        }

        #region draw methods

        private bool DrawLogItem(in Rect rect, in TempLogItem logItem, bool isOdd, bool isSelected)
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
            Rect iconRect = new Rect(rect.x, rect.y, LOG_ITEM_ICON_WIDTH, LOG_ITEM_HEIGHT);
            Rect logRect = new Rect(rect.x + iconRect.width, rect.y, rect.width - LOG_ITEM_ICON_WIDTH, LOG_ITEM_HEIGHT);
            result1 = GUI.Button(iconRect, m_boxIcon, m_boxIconStyle);
            result2 = GUI.Button(logRect, $"[{ logItem.LogTime}] {logItem.LogMessage}", m_boxItemStyle); //, GUILayout.ExpandWidth(true)
            return result1 || result2;
        }

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

            m_isClearOnPlay = GUILayout.Toggle(TempLogManagerForUnityEditor.IsClearOnPlay, new GUIContent("Clear On Play"), EditorStyles.toolbarButton, GUILayout.Width(80.0f));
            TempLogManagerForUnityEditor.IsClearOnPlay = m_isClearOnPlay;
            // m_isErrorPause = GUILayout.Toggle(LogManagerForUnityEditor.IsErrorPause, new GUIContent("Error Pause"), EditorStyles.toolbarButton, GUILayout.Width(70.0f));
            // LogManagerForUnityEditor.IsErrorPause = m_isErrorPause;

            m_writeFileInEditorMode = GUILayout.Toggle(m_writeFileInEditorMode, new GUIContent("Write Log File"), EditorStyles.toolbarButton, GUILayout.Width(120.0f));
            TempLogManagerForUnityEditor.SetWriteFileFlag(m_writeFileInEditorMode);

            GUILayout.Space(30.0f);
            if (GUILayout.Button(new GUIContent("Clear Log File"), EditorStyles.toolbarButton, GUILayout.Width(120.0f)))
            {
                TempLogManager.ClearLogFiles();
            }

            GUILayout.FlexibleSpace();

            m_normalLogCount = Mathf.Clamp(TempLogManagerForUnityEditor.InfoLogCount, 0, 100);
            m_warningLogCount = Mathf.Clamp(TempLogManagerForUnityEditor.WarningLogCount, 0, 100);
            m_errorLogCount = Mathf.Clamp(TempLogManagerForUnityEditor.ErrorLogCount, 0, 100);

            m_isShowLog = GUILayout.Toggle(TempLogManagerForUnityEditor.IsShowLog, new GUIContent(TempLogManagerHelper.GetNumberStr(m_normalLogCount), m_infoIconSmall), EditorStyles.toolbarButton, GUILayout.Width(LOG_FLAG_SIZE));
            m_isShowWarning = GUILayout.Toggle(TempLogManagerForUnityEditor.IsShowWarning, new GUIContent(TempLogManagerHelper.GetNumberStr(m_warningLogCount), m_warningIconSmall), EditorStyles.toolbarButton, GUILayout.Width(LOG_FLAG_SIZE));
            m_isShowError = GUILayout.Toggle(TempLogManagerForUnityEditor.IsShowError, new GUIContent(TempLogManagerHelper.GetNumberStr(m_errorLogCount), m_errorIconSmall), EditorStyles.toolbarButton, GUILayout.Width(LOG_FLAG_SIZE));
            TempLogManagerForUnityEditor.IsShowLog = m_isShowLog;
            TempLogManagerForUnityEditor.IsShowWarning = m_isShowWarning;
            TempLogManagerForUnityEditor.IsShowError = m_isShowError;

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

            // draw bk
            GUI.DrawTexture(m_upperPanel, m_boxItemStyle.normal.background);

            float scrollbarWidth = GUI.skin.verticalScrollbar.fixedWidth;
            Rect scrollbarRect = new Rect(m_upperPanel.x + m_upperPanel.width - scrollbarWidth, m_upperPanel.y, scrollbarWidth, m_upperPanel.height);
            Rect currentRect = new Rect(m_upperPanel.x, m_upperPanel.y, m_upperPanel.width - scrollbarWidth, m_upperPanel.height);
            float viewportHeight = m_upperPanel.height;
            int elementCount = m_currentShowCount;
            int showCount = Mathf.CeilToInt(currentRect.height / LOG_ITEM_HEIGHT);
            showCount = showCount > elementCount ? elementCount : showCount;

            // check for auto scroll
            if (showCount < m_currentShowCount && m_prevCount < m_currentShowCount)
            {
                if (m_isAutoScroll)
                {
                    m_scrollPosition = (m_currentShowCount * m_boxItemStyle.fixedHeight) - m_upperPanel.height;
                    m_isAutoScroll = false;
                }
                else
                {
                    float tempGap = (m_prevCount * m_boxItemStyle.fixedHeight) - m_upperPanel.height;
                    if (tempGap > 0)
                    {
                        if (tempGap - m_scrollPosition <= m_boxItemStyle.fixedHeight || m_isAutoScroll)
                        {
                            m_scrollPosition = (m_currentShowCount * m_boxItemStyle.fixedHeight) - m_upperPanel.height;
                        }
                    }
                }
            }
            else if (m_currentShowCount * m_boxItemStyle.fixedHeight < m_upperPanel.height)
            {
                m_isAutoScroll = true;
            }
            m_prevCount = m_currentShowCount;

            GUI.BeginClip(currentRect); // to clip the overflow stuff
            int indexOffset = Mathf.FloorToInt(m_scrollPosition / LOG_ITEM_HEIGHT);

            float startPosY = (indexOffset * LOG_ITEM_HEIGHT) - m_scrollPosition;

            int index = 0;
            for (int i = 0; i < showCount; i++)
            {
                Rect elementRect = new Rect(m_upperPanel.x, 0 + startPosY + i * LOG_ITEM_HEIGHT, currentRect.width, LOG_ITEM_HEIGHT);
                index = indexOffset + i;
                // TODO : fix GUI CLIP ERROR
                if (index < 0 || index > m_currentShowingItems.Count - 1)
                    break;
                if (DrawLogItem(elementRect, m_currentShowingItems[index], 0 == i % 2, m_currentShowingItems[index].IsSelected))
                {
                    if (null != m_selectedLogItem)
                    {
                        if (m_currentShowingItems[index] == m_selectedLogItem)
                        {
                            // click a some one, open code
                            JumpToStackTop();
                        }
                        else
                        {
                            m_selectedLogItem.IsSelected = false;
                            m_selectedLogItem = m_currentShowingItems[index];
                            TempLogManagerForUnityEditor.SetSelectedItem(m_selectedLogItem);
                            m_selectedLogItem.IsSelected = true;
                        }
                    }
                    else
                    {
                        m_selectedLogItem = m_currentShowingItems[indexOffset + i];
                        TempLogManagerForUnityEditor.SetSelectedItem(m_selectedLogItem);
                        m_selectedLogItem.IsSelected = true;
                    }
                    GUI.changed = true;
                }
            }

            GUI.EndClip();

            // do stuff for scroller
            float scrollSensitivity = LOG_ITEM_HEIGHT;
            float fullElementHeight = elementCount * LOG_ITEM_HEIGHT;
            float maxScrollPos = (fullElementHeight > currentRect.height) ? (fullElementHeight - currentRect.height) : 0;

            m_scrollPosition = Mathf.Max(0, GUI.VerticalScrollbar(scrollbarRect, m_scrollPosition, currentRect.height, 0, Mathf.Max(fullElementHeight, currentRect.height)));
            int controlId = GUIUtility.GetControlID(FocusType.Passive);
            if (EventType.ScrollWheel == Event.current.GetTypeForControl(controlId))
            {
                Vector2 mousePos = Event.current.mousePosition;
                if (mousePos.x >= m_upperPanel.x && mousePos.y >= m_upperPanel.y && mousePos.x <= m_upperPanel.width && mousePos.y < m_upperPanel.height)
                {
                    m_scrollPosition = Mathf.Clamp(m_scrollPosition + Event.current.delta.y * scrollSensitivity, 0, maxScrollPos);
                    Event.current.Use();
                }
            }

        }

        private void DrawLowerPanel()
        {
            float yPos = GetPanelGroupHeight() * m_upperSizeRatio + MENU_BAR_HEIGHT + RESIZER_HEIGHT;
            m_lowerPanel = new Rect(0, yPos, this.position.width, GetPanelGroupHeight() * (1.0f - m_upperSizeRatio));
            GUILayout.BeginArea(m_lowerPanel, m_panelStyle);

            m_lowerPanelScroll = GUILayout.BeginScrollView(m_lowerPanelScroll);

            string logDetail = null;
            string[] logDetailMutiLine = null;

            string pathline = "";
            string tempCase = ".cs:";
            string path = string.Empty;
            int line = 0;
            int splitwa = 0;

            if (null != m_selectedLogItem)
            {
                logDetail = m_selectedLogItem.LogStackTrace;
                GUILayout.TextArea(string.Format("{0}\n", m_selectedLogItem.LogMessage), m_textAreaStyle);

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

        #endregion draw methods

        private void ProcessEvents(Event currentEvent)
        {
            // check if mouse in panel
            Vector2 mousePos = currentEvent.mousePosition; // pos for this panel
            Rect selfRect = this.position;
            if (mousePos.x < 0 || mousePos.y < 0 || mousePos.x > selfRect.width || mousePos.y > selfRect.height)
            {
                // mouse is not on the pancel. finish all event 
                m_isResizing = false;
                return;
            }

            if (EventType.MouseDown == currentEvent.type)
            {
                // if press mouse left in resizer
                m_isResizing = (0 == currentEvent.button && m_resizer.Contains(currentEvent.mousePosition));
            }
            else if (EventType.MouseUp == currentEvent.type)
            {
                m_isResizing = false;
            }

            if (EventType.KeyDown == currentEvent.type)
            {
                if (KeyCode.UpArrow == currentEvent.keyCode)
                {
                    // select prev log
                }
                else if (KeyCode.DownArrow == currentEvent.keyCode)
                {
                    // select next log
                }
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
                TempLogManagerForUnityEditor.UpperSizeRatio = m_upperSizeRatio;
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
            m_boxIconStyle.fixedHeight = LOG_ITEM_HEIGHT;
            m_boxIconStyle.fixedWidth = LOG_ITEM_ICON_WIDTH;

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
            TempLogManagerForUnityEditor.GetLogs(ref m_currentShowingItems);
            m_normalLogCount = TempLogManagerForUnityEditor.InfoLogCount;
            m_warningLogCount = TempLogManagerForUnityEditor.WarningLogCount;
            m_errorLogCount = TempLogManagerForUnityEditor.ErrorLogCount;

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

            m_isShowLog = TempLogManagerForUnityEditor.IsShowLog;
            m_isShowWarning = TempLogManagerForUnityEditor.IsShowWarning;
            m_isShowError = TempLogManagerForUnityEditor.IsShowError;
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
            m_isClearOnPlay = TempLogManagerForUnityEditor.IsClearOnPlay;
            m_isShowLog = TempLogManagerForUnityEditor.IsShowLog;
            m_isShowWarning = TempLogManagerForUnityEditor.IsShowWarning;
            m_isShowError = TempLogManagerForUnityEditor.IsShowError;
        }

        #region life circle

        private void Awake()
        {
            m_upperSizeRatio = TempLogManagerForUnityEditor.GetInitPack().UpperPanelSizeRatio;
            OnTempConsoleCreated?.Invoke();
            _instance = this;
        }

        private void OnEnable()
        {
            m_hasInited = false;
            GetAssets();
            ContainerInit();

            InitSomeStuff();
            TempLogManagerForUnityEditor.OnLogsUpdated += WannaRepaint;
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
            TempLogManagerForUnityEditor.OnLogsUpdated -= WannaRepaint;
        }

        private void OnDestroy()
        {
            OnTempConsoleDestroyed?.Invoke();
            _instance = null;
        }

        #endregion

    }

}
