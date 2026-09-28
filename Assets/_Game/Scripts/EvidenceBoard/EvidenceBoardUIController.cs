using System;
using TMPro;
using UnityEngine;

public class EvidenceBoardUIController : MonoBehaviour
{
    [Serializable]
    private class EvidenceCardData
    {
        [SerializeField] private string title;
        [SerializeField, TextArea(2, 5)] private string description;

        public string Title => title;
        public string Description => description;

        public EvidenceCardData(string title, string description)
        {
            this.title = title;
            this.description = description;
        }
    }

    [Header("Panels")]
    [SerializeField] private GameObject boardRoot;
    [SerializeField] private GameObject boardPanel;
    [SerializeField] private GameObject evidenceDetailsPanel;

    [Header("Details")]
    [SerializeField] private TMP_Text evidenceTitleText;
    [SerializeField] private TMP_Text evidenceDescriptionText;

    [Header("Evidence Cards")]
    [SerializeField] private EvidenceCardData[] evidenceCards =
    {
        new EvidenceCardData("Victim Photo", "Photo taken at the crime scene"),
        new EvidenceCardData("Parking Receipt", "Receipt timestamp 22 41"),
        new EvidenceCardData("Witness Statement", "Witness claims the suspect was home")
    };

    private bool isInitialized;
    private bool referencesValid;

    public bool IsOpen { get; private set; }

    public event Action ExitRequested;

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (isInitialized)
        {
            return;
        }

        isInitialized = true;
        referencesValid = HasRequiredReferences();

        if (!referencesValid)
        {
            Debug.LogError("EvidenceBoardUIController няма зададени нужните references или три evidence cards", this);
        }

        if (boardRoot != null)
        {
            boardRoot.SetActive(false);
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

    public bool ShowBoard()
    {
        Initialize();

        if (!referencesValid || IsOpen)
        {
            return false;
        }

        // отваря таблото
        IsOpen = true;
        boardRoot.SetActive(true);
        BackToBoard();
        return true;
    }

    public void ShowEvidenceDetails(int evidenceIndex)
    {
        if (!IsOpen || evidenceIndex < 0 || evidenceIndex >= evidenceCards.Length)
        {
            return;
        }

        EvidenceCardData evidence = evidenceCards[evidenceIndex];

        if (evidence == null)
        {
            Debug.LogError($"Evidence card {evidenceIndex} няма зададени данни", this);
            return;
        }

        // показва доказателството
        evidenceTitleText.text = evidence.Title;
        evidenceDescriptionText.text = evidence.Description;
        boardPanel.SetActive(false);
        evidenceDetailsPanel.SetActive(true);
    }

    public void BackToBoard()
    {
        if (!IsOpen)
        {
            return;
        }

        // връща таблото
        evidenceDetailsPanel.SetActive(false);
        boardPanel.SetActive(true);
    }

    public void ExitBoard()
    {
        if (!IsOpen)
        {
            return;
        }

        CloseBoard();
        ExitRequested?.Invoke();
    }

    public void CloseBoard()
    {
        if (!IsOpen)
        {
            return;
        }

        // затваря таблото
        IsOpen = false;
        evidenceDetailsPanel.SetActive(false);
        boardPanel.SetActive(true);
        boardRoot.SetActive(false);
    }

    private bool HasRequiredReferences()
    {
        if (boardRoot == null
            || boardPanel == null
            || evidenceDetailsPanel == null
            || evidenceTitleText == null
            || evidenceDescriptionText == null
            || evidenceCards == null
            || evidenceCards.Length < 3)
        {
            return false;
        }

        foreach (EvidenceCardData evidence in evidenceCards)
        {
            if (evidence == null)
            {
                return false;
            }
        }

        return true;
    }
}
