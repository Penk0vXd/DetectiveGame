using System;
using TMPro;
using UnityEngine;

public class PhoneUIController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject phoneRoot;
    [SerializeField] private GameObject contactsPanel;
    [SerializeField] private GameObject callPanel;

    [Header("Contact Content")]
    [SerializeField] private TMP_Text contactNameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private string contactName = string.Empty;
    [SerializeField, TextArea(3, 8)] private string dialogue = string.Empty;

    public bool IsOpen { get; private set; }

    public event Action ExitRequested;

    private void Awake()
    {
        if (!HasRequiredReferences())
        {
            Debug.LogError("PhoneUIController няма зададени нужните references", this);
        }

        if (phoneRoot != null)
        {
            phoneRoot.SetActive(false);
        }
    }

    private void OnDisable()
    {
        if (!IsOpen)
        {
            return;
        }

        IsOpen = false;
        ExitRequested?.Invoke();
    }

    public bool OpenPhone()
    {
        if (!HasRequiredReferences() || IsOpen)
        {
            return false;
        }

        contactNameText.text = contactName;
        dialogueText.text = dialogue;

        IsOpen = true;
        phoneRoot.SetActive(true);
        ShowContacts();
        return true;
    }

    public void ShowContacts()
    {
        if (!IsOpen)
        {
            return;
        }

        // показва контактите
        callPanel.SetActive(false);
        contactsPanel.SetActive(true);
    }

    public void StartCall()
    {
        if (!IsOpen)
        {
            return;
        }

        // започва разговора
        contactsPanel.SetActive(false);
        callPanel.SetActive(true);
    }

    public void HangUp()
    {
        if (!IsOpen)
        {
            return;
        }

        // затваря разговора
        ShowContacts();
    }

    public void ExitPhone()
    {
        if (!IsOpen)
        {
            return;
        }

        ClosePhone();
        ExitRequested?.Invoke();
    }

    public void ClosePhone()
    {
        if (!IsOpen)
        {
            return;
        }

        IsOpen = false;
        callPanel.SetActive(false);
        contactsPanel.SetActive(true);
        phoneRoot.SetActive(false);
    }

    private bool HasRequiredReferences()
    {
        return phoneRoot != null
            && contactsPanel != null
            && callPanel != null
            && contactNameText != null
            && dialogueText != null;
    }
}
