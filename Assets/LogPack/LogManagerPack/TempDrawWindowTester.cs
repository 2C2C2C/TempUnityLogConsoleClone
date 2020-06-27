using System.Collections.Generic;
using UnityEngine;

namespace TempDraw
{
    [System.Serializable]
    public struct TempDrawData
    {
        public int IconTag;
        public string TempMessage;
    }

    public class TempDrawWindowTester : MonoBehaviour
    {
        private TempDrawWindowTester _instance = null;
        public TempDrawWindowTester Instance => _instance;

        List<TempDrawData> m_data = null;

        public static event System.Action<List<TempDrawData>> OnDataSpread;


        [Button("set data to window")]
        public void SetDataToWindow()
        {
            OnDataSpread?.Invoke(m_data);
        }

        [Button("print data")]
        public void PrintAllData()
        {
            if (null != m_data)
            {
                for (int i = 0; i < m_data.Count; i++)
                {
                    Debug.Log($"Tag: {m_data[i].IconTag} ; Message: {m_data[i].TempMessage}");
                }
            }
        }

        [Button("add 1 data")]
        public void Add1Data()
        {
            if (null == m_data)
                InitContainer();

            TempDrawData data = new TempDrawData();
            data.IconTag = Random.Range(0, 2);
            data.TempMessage = $"test data {m_data.Count + 1}";
            m_data.Add(data);
        }

        [Button("init container")]
        private void InitContainer()
        {
            if (null == m_data)
                m_data = new List<TempDrawData>();
            else
                m_data.Clear();
        }

        #region mono method

        private void Reset()
        {
            if (null == Instance)
            {
                _instance = this;
            }
        }

        private void Awake()
        {
            if (null == m_data)
                InitContainer();
        }

        #endregion

    }
}