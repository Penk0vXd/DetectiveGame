using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EvidenceBoardUIController : MonoBehaviour
{
    [Serializable]
    private class EvidenceCardData
    {
        [SerializeField] private string title;
        [SerializeField, TextArea(2, 5)] private string description;
        [SerializeField] private string type;
        [SerializeField] private string source;

        public string Title => title;
        public string Description => description;
        public string Type => type;
        public string Source => source;

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
    [SerializeField] private RectTransform[] cardRects;
    [SerializeField] private TMP_Text[] cardProvenanceTexts;
    [SerializeField] private EvidenceCardData[] evidenceCards =
    {
        new EvidenceCardData("Снимка на жертвата", "Снимка, направена на местопрестъплението"),
        new EvidenceCardData("Бележка от паркинг", "Отпечатаният час върху бележката е 22:41"),
        new EvidenceCardData("Показание — Елена Мур", "Елена Мур съобщава за шум в коридора, но не посочва източника му.")
    };

    private bool isInitialized;
    private bool referencesValid;
    private EvidenceBoardSession session;
    private RectTransform linksLayer;
    private TMP_InputField notesInput;
    private TMP_Text statusText;
    private TMP_Text detailProvenanceText;
    private Button cancelLinkButton;
    private Button[] removeButtons;
    private TMP_Text[] removeLabels;
    private bool linking;
    private int firstLinkCard = -1;
    private Vector2 lastPanelSize;
    private static readonly Vector2Int[] LinkPairs =
    {
        new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(1, 2)
    };

    public bool IsOpen { get; private set; }
    public bool CanOpen => isActiveAndEnabled && HasRequiredReferences() && !IsOpen;

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
        else
        {
            session = new EvidenceBoardSession(evidenceCards.Length);
            BuildSessionUI();
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

        CloseBoard();
        ExitRequested?.Invoke();
    }

    private void Update()
    {
        if (!IsOpen || boardRoot == null || boardRoot.activeInHierarchy)
            return;

        // Външно скрит UI root освобождава активния инструмент.
        CloseBoard();
        ExitRequested?.Invoke();
    }

    private void LateUpdate()
    {
        if (!IsOpen || !boardPanel.activeInHierarchy) return;
        Vector2 size = ((RectTransform)boardPanel.transform).rect.size;
        if (size == lastPanelSize) return;
        lastPanelSize = size;
        DrawLinks();
    }

    public bool ShowBoard()
    {
        Initialize();

        if (!CanOpen)
        {
            return false;
        }

        // отваря таблото
        IsOpen = true;
        boardRoot.SetActive(true);
        BackToBoard();
        notesInput.SetTextWithoutNotify(session.Notes);
        RefreshLinks();
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

        if (linking)
        {
            SelectLinkCard(evidenceIndex);
            return;
        }

        // показва доказателството
        evidenceTitleText.text = evidence.Title;
        evidenceDescriptionText.text = evidence.Description;
        detailProvenanceText.text = $"{evidence.Type.ToUpperInvariant()} · Източник: {evidence.Source}";
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
        CancelLink();
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
            || evidenceCards.Length != 3
            || cardRects == null || cardRects.Length != 3
            || cardProvenanceTexts == null || cardProvenanceTexts.Length != 3)
        {
            return false;
        }

        foreach (EvidenceCardData evidence in evidenceCards)
        {
            if (evidence == null || string.IsNullOrWhiteSpace(evidence.Type)
                || string.IsNullOrWhiteSpace(evidence.Source))
            {
                return false;
            }
        }

        foreach (RectTransform card in cardRects)
            if (card == null) return false;
        foreach (TMP_Text provenance in cardProvenanceTexts)
            if (provenance == null) return false;

        return true;
    }

    private void BuildSessionUI()
    {
        // shortcut: три фиксирани карти и три двойки; смени layout-а при динамично case съдържание.
        RectTransform panel = (RectTransform)boardPanel.transform;
        for (int i = 0; i < 3; i++)
        {
            float top = .98f - i * .18f;
            cardRects[i].anchorMin = new Vector2(.06f, top - .17f);
            cardRects[i].anchorMax = new Vector2(.94f, top);
            cardRects[i].anchoredPosition = Vector2.zero;
            cardRects[i].sizeDelta = Vector2.zero;
            cardProvenanceTexts[i].fontSize = 18;
            cardProvenanceTexts[i].text = $"{evidenceCards[i].Type.ToUpperInvariant()} · {evidenceCards[i].Source}";
        }

        linksLayer = Rect(panel, "BoardLinks", Vector2.zero, Vector2.one);
        linksLayer.SetAsFirstSibling();
        Button linkButton = Button(panel, "LinkCardsButton", "СВЪРЖИ", new Vector2(.06f, .38f),
            new Vector2(.45f, .44f), BeginLink);
        cancelLinkButton = Button(panel, "CancelLinkButton", "ОТКАЖИ", new Vector2(.55f, .38f),
            new Vector2(.94f, .44f), CancelLink);
        statusText = Label(panel, "BoardLinkStatus", "", new Vector2(.06f, .34f),
            new Vector2(.94f, .38f), 17, TextAlignmentOptions.Center);
        removeButtons = new Button[LinkPairs.Length];
        removeLabels = new TMP_Text[LinkPairs.Length];
        for (int i = 0; i < LinkPairs.Length; i++)
        {
            Vector2Int pair = LinkPairs[i];
            float top = .34f - i * .05f;
            removeButtons[i] = Button(panel, "RemoveLink" + i, "", new Vector2(.06f, top - .05f),
                new Vector2(.94f, top), () => RemoveLink(pair.x, pair.y));
            removeLabels[i] = removeButtons[i].GetComponentInChildren<TMP_Text>();
            removeLabels[i].fontSize = 15;
        }
        Label(panel, "BoardNotesHeader", "МОИ БЕЛЕЖКИ · Временно — само за тази сесия",
            new Vector2(.06f, .14f), new Vector2(.94f, .19f), 16, TextAlignmentOptions.Left);
        BuildNotesInput(panel);
        detailProvenanceText = Label((RectTransform)evidenceDetailsPanel.transform, "BoardDetailSource", "",
            new Vector2(.07f, .19f), new Vector2(.93f, .30f), 18, TextAlignmentOptions.Left);
        cancelLinkButton.gameObject.SetActive(false);
        linkButton.onClick.AddListener(() => cancelLinkButton.gameObject.SetActive(linking));
    }

    private void BuildNotesInput(RectTransform panel)
    {
        RectTransform rect = Rect(panel, "BoardNotesInput", new Vector2(.06f, .02f), new Vector2(.94f, .14f));
        rect.gameObject.AddComponent<Image>().color = new Color(.12f, .14f, .14f, .96f);
        rect.gameObject.AddComponent<RectMask2D>();
        TMP_Text value = Label(rect, "Text", "", new Vector2(.02f, .06f), new Vector2(.98f, .94f),
            18, TextAlignmentOptions.TopLeft);
        TMP_Text placeholder = Label(rect, "Placeholder", "Напишете собствена бележка…",
            new Vector2(.02f, .06f), new Vector2(.98f, .94f), 18, TextAlignmentOptions.TopLeft);
        placeholder.color = new Color(.65f, .68f, .64f);
        notesInput = rect.gameObject.AddComponent<TMP_InputField>();
        notesInput.textViewport = rect;
        notesInput.textComponent = value;
        notesInput.placeholder = placeholder;
        notesInput.lineType = TMP_InputField.LineType.MultiLineNewline;
        notesInput.characterLimit = 1000;
        notesInput.onValueChanged.AddListener(text => session.Notes = text);
    }

    private void BeginLink()
    {
        linking = true;
        firstLinkCard = -1;
        statusText.text = "Изберете първа карта.";
    }

    private void CancelLink()
    {
        linking = false;
        firstLinkCard = -1;
        if (statusText != null) statusText.text = "";
        if (cancelLinkButton != null) cancelLinkButton.gameObject.SetActive(false);
    }

    private void SelectLinkCard(int index)
    {
        if (firstLinkCard < 0)
        {
            firstLinkCard = index;
            statusText.text = "Изберете втора карта.";
            return;
        }
        if (firstLinkCard == index) statusText.text = "Изберете две различни карти.";
        else if (session.TryLink(firstLinkCard, index)) statusText.text = "Връзката е добавена.";
        else statusText.text = "Тази връзка вече съществува.";
        linking = false;
        firstLinkCard = -1;
        cancelLinkButton.gameObject.SetActive(false);
        RefreshLinks();
    }

    private void RemoveLink(int first, int second)
    {
        if (!session.RemoveLink(first, second)) return;
        statusText.text = "Връзката е премахната.";
        RefreshLinks();
    }

    private void RefreshLinks()
    {
        for (int i = 0; i < LinkPairs.Length; i++)
        {
            Vector2Int pair = LinkPairs[i];
            removeButtons[i].gameObject.SetActive(session.HasLink(pair.x, pair.y));
            removeLabels[i].text = $"{evidenceCards[pair.x].Type} ↔ {evidenceCards[pair.y].Type} · Премахни";
        }
        DrawLinks();
    }

    private void DrawLinks()
    {
        foreach (Transform child in linksLayer)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
        RectTransform panel = (RectTransform)boardPanel.transform;
        foreach ((int first, int second) in session.Links)
        {
            bool left = first == 0 && second == 2;
            float gutter = left ? panel.rect.xMin + panel.rect.width * .025f
                : panel.rect.xMax - panel.rect.width * (first == 0 ? .025f : .045f);
            Vector2 a = CardEdge(first, left);
            Vector2 b = CardEdge(second, left);
            Segment(a, new Vector2(gutter, a.y));
            Segment(new Vector2(gutter, a.y), new Vector2(gutter, b.y));
            Segment(new Vector2(gutter, b.y), b);
        }
    }

    private Vector2 CardEdge(int index, bool left)
    {
        RectTransform card = cardRects[index];
        Vector3 point = new Vector3(left ? card.rect.xMin : card.rect.xMax, card.rect.center.y, 0f);
        return ((RectTransform)boardPanel.transform).InverseTransformPoint(card.TransformPoint(point));
    }

    private void Segment(Vector2 start, Vector2 end)
    {
        RectTransform rect = Rect(linksLayer, "PlayerLink", new Vector2(.5f, .5f), new Vector2(.5f, .5f));
        rect.anchoredPosition = (start + end) * .5f;
        Vector2 delta = end - start;
        rect.sizeDelta = new Vector2(delta.magnitude, 3f);
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        Image line = rect.gameObject.AddComponent<Image>();
        line.color = new Color(.74f, .77f, .70f, .9f);
        line.raycastTarget = false;
    }

    private static RectTransform Rect(Transform parent, string name, Vector2 min, Vector2 max)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    private TMP_Text Label(Transform parent, string name, string value, Vector2 min, Vector2 max,
        float fontSize, TextAlignmentOptions alignment)
    {
        TMP_Text label = Rect(parent, name, min, max).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = evidenceTitleText.font;
        label.fontSize = fontSize;
        label.alignment = alignment;
        label.color = new Color(.94f, .92f, .85f);
        label.raycastTarget = false;
        label.text = value;
        return label;
    }

    private Button Button(Transform parent, string name, string value, Vector2 min, Vector2 max,
        UnityEngine.Events.UnityAction click)
    {
        RectTransform rect = Rect(parent, name, min, max);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(.15f, .19f, .19f, .96f);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(click);
        Label(rect, "Label", value, Vector2.zero, Vector2.one, 18, TextAlignmentOptions.Center);
        return button;
    }
}
