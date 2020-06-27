using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TempDraw
{
    public class TempDrawWindow : EditorWindow
    {
        public static string WINDOW_NAME = "Temp Draw Window";
        private static TempDrawWindow _instance = null;
        public static TempDrawWindow Instance => _instance;

        private List<TempDrawData> m_data = null;
        private float m_scrollPosition = 0.0f;
        private const int NON_SELECTED_INDEX = -1;
        private int m_selectedIndex = -1;

        #region temp element
        private const float ELEMENT_HEIGHT = 20.0f;
        private const float ELEMENT_ICON_SIZE = 20.0f;
        #endregion

        #region icons
        private Texture2D m_infoIconSmall = null;
        private Texture2D m_warningIconSmall = null;
        #endregion

        [MenuItem("Window/Temp Draw Window")]
        private static void OpenWindow()
        {
            TempDrawWindow window = GetWindow<TempDrawWindow>();
            Texture2D icon = EditorGUIUtility.Load("icons/UnityEditor.ConsoleWindow.png") as Texture2D;
            window.titleContent = new GUIContent(WINDOW_NAME, icon);
        }

        public void SetData(List<TempDrawData> data)
        {
            if (null == m_data)
                m_data = new List<TempDrawData>();
            else
                m_data.Clear();

            m_data.AddRange(data);
        }

        private void DrawTempElement(Rect elementRect, int dataIndex)
        {
            Rect iconRect = new Rect(elementRect.x, elementRect.y, ELEMENT_ICON_SIZE, elementRect.height);
            if (m_data[dataIndex].IconTag == 0)
                GUI.Label(iconRect, m_infoIconSmall);
            else
                GUI.Label(iconRect, m_warningIconSmall);


            var textAreaStyle = new GUIStyle();
            textAreaStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
            textAreaStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/projectbrowsericonareabg.png") as Texture2D;
            var labelButtonStyle = new GUIStyle();
            labelButtonStyle.normal.background = textAreaStyle.normal.background;
            labelButtonStyle.alignment = TextAnchor.MiddleLeft;
            labelButtonStyle.stretchWidth = false;
            var b = labelButtonStyle.border;
            b.left = 0;
            b.right = 0;
            b.top = 0;
            b.bottom = 0;
            labelButtonStyle.border = b;

            if (m_selectedIndex == dataIndex)
            {
                labelButtonStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/menuitemhover.png") as Texture2D;
            }

            Rect labelRect = new Rect(elementRect.x + ELEMENT_ICON_SIZE, elementRect.y, elementRect.width - ELEMENT_ICON_SIZE, elementRect.height);
            GUI.Label(labelRect, $"Tag: {m_data[dataIndex].IconTag} ; Message: {m_data[dataIndex].TempMessage} ;");

            bool click = GUI.Button(labelRect, new GUIContent(m_data[dataIndex].TempMessage), labelButtonStyle);
            if (click)
            {
                m_selectedIndex = dataIndex;
            }
        }

        private void DrawTempRect()
        {
            // only use half space of this shit window
            Rect viewportRect = new Rect(0.0f, 0.0f, this.position.width, this.position.height * 0.5f);

            float scrollbarWidth = GUI.skin.verticalScrollbar.fixedWidth;
            Rect scrollbarRect = new Rect(viewportRect.x + viewportRect.width - scrollbarWidth, viewportRect.y, scrollbarWidth, viewportRect.height);
            Rect currentRect = new Rect(0.0f, 0.0f, viewportRect.width - scrollbarWidth, viewportRect.height);
            float viewportHeight = viewportRect.height;
            int elementCount = m_data.Count;

            GUI.BeginClip(currentRect); // to clip the overflow stuff
            int indexOffset = Mathf.FloorToInt(m_scrollPosition / ELEMENT_HEIGHT);
            int showCount = Mathf.CeilToInt(currentRect.height / ELEMENT_HEIGHT);
            showCount = showCount > elementCount ? elementCount : showCount;
            float startPosY = (indexOffset * ELEMENT_HEIGHT) - m_scrollPosition;

            for (int i = 0; i < showCount; i++)
            {
                Rect elementRect = new Rect(0, 0 + startPosY + i * ELEMENT_HEIGHT, currentRect.width, ELEMENT_HEIGHT);
                DrawTempElement(elementRect, indexOffset + i);
            }
            GUI.EndClip();

            // do stuff for scroller
            float scrollSensitivity = ELEMENT_HEIGHT;
            float fullElementHeight = elementCount * ELEMENT_HEIGHT;
            float maxScrollPos = (fullElementHeight > currentRect.height) ? (fullElementHeight - currentRect.height) : 0;

            m_scrollPosition = Mathf.Max(0, GUI.VerticalScrollbar(scrollbarRect, m_scrollPosition, currentRect.height, 0, Mathf.Max(fullElementHeight, currentRect.height)));
            int controlId = GUIUtility.GetControlID(FocusType.Passive);
            if (EventType.ScrollWheel == Event.current.GetTypeForControl(controlId))
            {
                m_scrollPosition = Mathf.Clamp(m_scrollPosition + Event.current.delta.y * scrollSensitivity, 0, maxScrollPos);
                Event.current.Use();
            }

        }

        private void GetAsset()
        {
            m_infoIconSmall = EditorGUIUtility.Load("icons/console.infoicon.sml.png") as Texture2D;
            m_warningIconSmall = EditorGUIUtility.Load("icons/console.warnicon.sml.png") as Texture2D;
        }

        #region life cycle

        private void Awake()
        {
            _instance = this;
            m_data = new List<TempDrawData>();
        }

        private void OnEnable()
        {
            TempDrawWindowTester.OnDataSpread -= SetData;
            TempDrawWindowTester.OnDataSpread += SetData;
            GetAsset();
        }

        private void OnGUI()
        {
            DrawTempRect();
        }

        private void OnDestroy()
        {
            TempDrawWindowTester.OnDataSpread -= SetData;
            _instance = null;
        }

        #endregion

    }
}