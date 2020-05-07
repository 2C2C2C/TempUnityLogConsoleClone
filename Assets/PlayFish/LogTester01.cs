
using UnityEngine;

public class LogTester01 : MonoBehaviour
{
    public LogType m_logType = default;
    public string m_logMsg = "this\nis\nmagic\ntest";

    [Button("test custom log")]
    public void TestCustomLog()
    {
        switch (m_logType)
        {
            case LogType.Assert:
            case LogType.Error:
            case LogType.Exception:
                Debug.LogError(m_logMsg);
                break;
            case LogType.Log:
                Debug.LogWarning(m_logMsg);
                break;
            case LogType.Warning:
                Debug.Log(m_logMsg);
                break;
            default:
                Debug.LogError(m_logMsg);
                break;
        }
    }

    public void TestEmptyMethod()
    {
        float wa;
    }

}


