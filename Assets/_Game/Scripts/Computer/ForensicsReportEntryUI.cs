using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class ForensicsReportEntryUI : MonoBehaviour
{
    [SerializeField] private Button button = null;
    [SerializeField] private TMP_Text reportNumberText = null;
    [SerializeField] private TMP_Text titleText = null;
    [SerializeField] private TMP_Text statusText = null;
    [SerializeField] private TMP_Text dateText = null;

    private ForensicsReportData reportData;
    private Action<ForensicsReportData> selectedCallback;

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (button != null)
        {
            button.onClick.AddListener(HandleClick);
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
        }
    }

    public void Initialize(ForensicsReportData data, Action<ForensicsReportData> onSelected)
    {
        reportData = data;
        selectedCallback = onSelected;

        SetText(reportNumberText, data == null ? string.Empty : data.ReportNumber);
        SetText(titleText, data == null ? string.Empty : data.Title);
        SetText(statusText, data == null ? string.Empty : data.Status);
        SetText(dateText, data == null ? string.Empty : data.Date);
    }

    private void HandleClick()
    {
        if (reportData != null)
        {
            selectedCallback?.Invoke(reportData);
        }
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
        {
            target.text = value ?? string.Empty;
        }
    }
}
