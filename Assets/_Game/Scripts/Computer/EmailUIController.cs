using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class EmailUIController : MonoBehaviour
{
    [Header("Mail Window")]
    [SerializeField] private GameObject mailWindow;
    [SerializeField] private Transform emailListContent;
    [SerializeField] private EmailEntryUI emailEntryPrefab;

    [Header("Email Details")]
    [SerializeField] private TMP_Text senderText;
    [SerializeField] private TMP_Text subjectText;
    [SerializeField] private TMP_Text dateText;
    [SerializeField] private TMP_Text bodyText;

    [Header("Email Data")]
    [SerializeField] private List<EmailData> emails = new List<EmailData>();

    private bool isInboxBuilt;
    private bool hasLoggedMissingWindow;
    private bool hasLoggedMissingInboxReferences;

    public void OpenMail()
    {
        if (mailWindow == null)
        {
            LogMissingWindowOnce();
            return;
        }

        // отваря mail
        mailWindow.SetActive(true);
        BuildInbox();
    }

    public void CloseMail()
    {
        if (mailWindow == null)
        {
            LogMissingWindowOnce();
            return;
        }

        // затваря mail
        mailWindow.SetActive(false);
    }

    public void SelectEmail(EmailData email)
    {
        if (email == null)
        {
            return;
        }

        // показва писмото
        SetText(senderText, email.Sender);
        SetText(subjectText, email.Subject);
        SetText(dateText, email.Date);
        SetText(bodyText, email.Body);
    }

    public void BuildInbox()
    {
        if (isInboxBuilt)
        {
            return;
        }

        if (emailListContent == null || emailEntryPrefab == null)
        {
            LogMissingInboxReferencesOnce();
            return;
        }

        if (emails == null)
        {
            isInboxBuilt = true;
            return;
        }

        // създава inbox
        foreach (EmailData email in emails)
        {
            if (email == null)
            {
                continue;
            }

            EmailEntryUI entry = Instantiate(emailEntryPrefab, emailListContent);
            entry.Initialize(email, SelectEmail);
        }

        isInboxBuilt = true;
    }

    private void LogMissingWindowOnce()
    {
        if (hasLoggedMissingWindow)
        {
            return;
        }

        hasLoggedMissingWindow = true;
        Debug.LogWarning("EmailUIController няма зададен MailWindow", this);
    }

    private void LogMissingInboxReferencesOnce()
    {
        if (hasLoggedMissingInboxReferences)
        {
            return;
        }

        hasLoggedMissingInboxReferences = true;
        Debug.LogWarning("EmailUIController няма зададени EmailListContent или EmailEntry prefab", this);
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }
}
