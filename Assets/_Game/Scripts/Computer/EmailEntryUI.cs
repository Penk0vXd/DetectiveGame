using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class EmailEntryUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text senderText;
    [SerializeField] private TMP_Text subjectText;
    [SerializeField] private TMP_Text dateText;

    private EmailData emailData;
    private Action<EmailData> selectedCallback;

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

    public void Initialize(EmailData data, Action<EmailData> onSelected)
    {
        emailData = data;
        selectedCallback = onSelected;

        if (data == null)
        {
            SetText(senderText, string.Empty);
            SetText(subjectText, string.Empty);
            SetText(dateText, string.Empty);
            return;
        }

        SetText(senderText, data.Sender);
        SetText(subjectText, data.Subject);
        SetText(dateText, data.Date);
    }

    private void HandleClick()
    {
        if (emailData == null)
        {
            return;
        }

        selectedCallback?.Invoke(emailData);
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }
}
