
using CustomLog;
using System;
using System.Collections.Generic;
using UnityEngine;

public class LogTester01 : MonoBehaviour
{
    [SerializeField]
    private LogType m_logType = default;
    [SerializeField]
    private string m_logMsg = "this \n is \n magic\n to \n test";
    [Button("test custom log")]
    public void TestCustomLog()
    {
        TempLogManager.CreateLog(in m_logMsg, m_logType);
    }

    [Button("test normal log")]
    public void TestNormalLog()
    {
        switch (m_logType)
        {
            case LogType.Error:
                Debug.LogError(m_logMsg);
                break;
            case LogType.Assert:
                Debug.LogError(m_logMsg);
                break;
            case LogType.Warning:
                Debug.LogWarning(m_logMsg);
                break;
            case LogType.Log:
                Debug.Log(m_logMsg);
                break;
            case LogType.Exception:
                Debug.LogError(m_logMsg);
                break;
            default:
                Debug.LogError(m_logMsg);
                break;
        }
    }

    private void TestCompileWarning()
    {
        int wa;
    }

    [Button("test DICT KEY excp")]
    public void CreateKeyExpc()
    {
        Dictionary<int, char> tempDict = new Dictionary<int, char>();

        tempDict.Add(1, '1');
        tempDict.Add(1, '1');
    }

    void OnEnable()
    {
        this.enabled = false;
    }

}
