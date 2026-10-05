using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class DatabaseResultEntryUI : MonoBehaviour
{
    [SerializeField] private Button button = null;
    [SerializeField] private TMP_Text fullNameText = null;
    [SerializeField] private TMP_Text dateOfBirthText = null;
    [SerializeField] private TMP_Text occupationText = null;

    private DatabasePersonData personData;
    private Action<DatabasePersonData> selectedCallback;

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

    public void Initialize(DatabasePersonData data, Action<DatabasePersonData> onSelected)
    {
        personData = data;
        selectedCallback = onSelected;

        SetText(fullNameText, data == null ? string.Empty : data.FullName);
        SetText(dateOfBirthText, data == null ? string.Empty : data.DateOfBirth);
        SetText(occupationText, data == null ? string.Empty : data.Occupation);
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
