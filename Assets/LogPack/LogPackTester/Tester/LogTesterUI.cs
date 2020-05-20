using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LogTesterUI : MonoBehaviour
{
    [SerializeField]
    private UnityEngine.UI.Text m_shownLog = null;

    [SerializeField]
    private UnityEngine.UI.InputField m_inpuField = null;

    [SerializeField]
    private UnityEngine.UI.Button m_logButton = null;
    [SerializeField]
    private UnityEngine.UI.Button m_logWarningButton = null;
    [SerializeField]
    private UnityEngine.UI.Button m_logErrorButton = null;

    private string m_logString = null;

    private void DoLog()
    {
        m_logString = m_inpuField.text;
        if (string.IsNullOrEmpty(m_logString) || string.IsNullOrWhiteSpace(m_logString))
            return;
        else
            CustomLog.TempLogManager.CreateLog(m_logString, LogType.Log);
    }

    private void DoLogWarning()
    {
        m_logString = m_inpuField.text;
        if (string.IsNullOrEmpty(m_logString) || string.IsNullOrWhiteSpace(m_logString))
            return;
        else
            CustomLog.TempLogManager.CreateLog(m_logString, LogType.Warning);
    }

    private void DoLogError()
    {
        m_logString = m_inpuField.text;
        if (string.IsNullOrEmpty(m_logString) || string.IsNullOrWhiteSpace(m_logString))
            return;
        else
            CustomLog.TempLogManager.CreateLog(m_logString, LogType.Error);
    }

    private void ShowLogText(string condition, string stackTrace, LogType type)
    {
        m_shownLog.text = $"[{type}] : {condition} {stackTrace} ";
    }

    #region mono method

    private void Awake()
    {
        CustomLog.TempLogManager.InitLogManager();

        m_logButton.onClick.AddListener(DoLog);
        m_logWarningButton.onClick.AddListener(DoLogWarning);
        m_logErrorButton.onClick.AddListener(DoLogError);

        Application.logMessageReceivedThreaded += ShowLogText;
    }

    private void OnEnable()
    {
        this.enabled = false;
    }

    private void OnDestroy()
    {
        Application.logMessageReceivedThreaded -= ShowLogText;

        m_logButton.onClick.RemoveAllListeners();
        m_logWarningButton.onClick.RemoveAllListeners();
        m_logErrorButton.onClick.RemoveAllListeners();
    }

    #endregion
}
