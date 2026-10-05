using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class CaseRecordEntryUI : MonoBehaviour
{
    [SerializeField] private Button button = null;
    [SerializeField] private TMP_Text titleText = null;
    [SerializeField] private TMP_Text recordTypeText = null;
    [SerializeField] private TMP_Text dateText = null;

    private CaseRecordData recordData;
    private Action<CaseRecordData> selectedCallback;

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

    public void Initialize(CaseRecordData data, Action<CaseRecordData> onSelected)
    {
        recordData = data;
        selectedCallback = onSelected;

        SetText(titleText, data == null ? string.Empty : data.Title);
        SetText(recordTypeText, data == null ? string.Empty : data.RecordType);
        SetText(dateText, data == null ? string.Empty : data.Date);
    }

    private void HandleClick()
    {
        if (recordData != null)
        {
            selectedCallback?.Invoke(recordData);
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
