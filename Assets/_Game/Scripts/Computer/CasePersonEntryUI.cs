using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class CasePersonEntryUI : MonoBehaviour
{
    [SerializeField] private Button button = null;
    [SerializeField] private TMP_Text nameText = null;
    [SerializeField] private TMP_Text roleText = null;

    private CasePersonData personData;
    private Action<CasePersonData> selectedCallback;

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

    public void Initialize(CasePersonData data, Action<CasePersonData> onSelected)
    {
        personData = data;
        selectedCallback = onSelected;

        SetText(nameText, data == null ? string.Empty : data.Name);
        SetText(roleText, data == null ? string.Empty : data.Role);
    }

    private void HandleClick()
    {
        if (personData != null)
        {
            selectedCallback?.Invoke(personData);
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
