using System;
using System.Collections.Generic;
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
    private enum BoardViewMode
    {
        Board,
        Timeline,
        Theory
    }

    private BoardPlayerNotesView playerNotesView;
    private BoardAttachedSourcesView attachedSourcesView;
    private BoardTimelineView timelineView;
    private BoardCaseTheoryView caseTheoryView;
    private Button boardModeTabButton;
    private Button timelineModeTabButton;
    private Button theoryModeTabButton;
    private CaseFilesUIController caseFilesController;
    private PhotosUIController photosController;
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
            playerNotesView = BoardPlayerNotesView.Create(boardRoot.transform, session.PlayerNotes, evidenceTitleText.font);
            attachedSourcesView = BoardAttachedSourcesView.Create(
                boardRoot.transform,
                playerNotesView.Frame,
                playerNotesView.Surface,
                session.AttachedSources,
                evidenceTitleText.font,
                ResolveSourceRecord,
                playerNotesView.SetHasExternalItems,
                OnAttachedDetailOpened,
                BackToBoard);
            timelineView = BoardTimelineView.Create(boardRoot.transform, session.Timeline, evidenceTitleText.font);
            caseTheoryView = BoardCaseTheoryView.Create(
                boardRoot.transform,
                session.CaseTheories,
                evidenceTitleText.font,
                GetCaseDocumentSources,
                sourceId => ResolveCaseDocumentSourceRecord(CaseTheorySession.DefaultCaseId, sourceId),
                CaseTheorySession.DefaultCaseId,
                GetCasePhotoSources,
                sourceId => ResolveCasePhotoSourceRecord(CaseTheorySession.DefaultCaseId, sourceId));
            BuildModeTabs();
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
        timelineView?.Hide();
        caseTheoryView?.Hide();
        UpdateModeTabVisuals(BoardViewMode.Board);
        attachedSourcesView?.CloseDetailsSilent();
        attachedSourcesView?.CancelRemove();
        evidenceTitleText.text = evidence.Title;
        evidenceDescriptionText.text = evidence.Description;
        detailProvenanceText.text = $"{evidence.Type.ToUpperInvariant()} — Източник: {evidence.Source}";
        boardPanel.SetActive(false);
        playerNotesView.gameObject.SetActive(false);
        evidenceDetailsPanel.SetActive(true);
    }

    public void BackToBoard()
    {
        if (!IsOpen)
        {
            return;
        }

        // връща таблото
        timelineView?.Hide();
        caseTheoryView?.Hide();
        UpdateModeTabVisuals(BoardViewMode.Board);
        attachedSourcesView?.CloseDetailsSilent();
        attachedSourcesView?.CancelRemove();
        evidenceDetailsPanel.SetActive(false);
        boardPanel.SetActive(true);
        playerNotesView.gameObject.SetActive(true);
    }

    public void ShowTimeline()
    {
        if (!IsOpen || timelineView == null)
        {
            return;
        }

        CancelLink();
        caseTheoryView?.Hide();
        attachedSourcesView?.CloseDetailsSilent();
        attachedSourcesView?.CancelRemove();
        evidenceDetailsPanel.SetActive(false);
        boardPanel.SetActive(false);
        playerNotesView.gameObject.SetActive(false);
        timelineView.Show();
        UpdateModeTabVisuals(BoardViewMode.Timeline);
    }

    public void ShowTheory()
    {
        if (!IsOpen || caseTheoryView == null)
        {
            return;
        }

        CancelLink();
        timelineView?.Hide();
        attachedSourcesView?.CloseDetailsSilent();
        attachedSourcesView?.CancelRemove();
        evidenceDetailsPanel.SetActive(false);
        boardPanel.SetActive(false);
        playerNotesView.gameObject.SetActive(false);
        caseTheoryView.Show();
        UpdateModeTabVisuals(BoardViewMode.Theory);
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
        timelineView?.Hide();
        caseTheoryView?.Hide();
        UpdateModeTabVisuals(BoardViewMode.Board);
        attachedSourcesView?.CloseDetailsSilent();
        attachedSourcesView?.CancelRemove();
        evidenceDetailsPanel.SetActive(false);
        boardPanel.SetActive(true);
        boardRoot.SetActive(false);
    }

    public bool IsSourceAttached(string sourceId, AttachedSourceType sourceType)
    {
        Initialize();
        return session != null && session.AttachedSources.ContainsSource(sourceId, sourceType);
    }

    public bool TryAttachSource(string sourceId, AttachedSourceType sourceType)
    {
        Initialize();
        if (session == null || string.IsNullOrWhiteSpace(sourceId))
        {
            return false;
        }

        Vector2 spawn = attachedSourcesView != null
            ? attachedSourcesView.NextSpawnPosition()
            : new Vector2(.75f, .25f);
        if (!session.AttachedSources.TryAttach(sourceId, sourceType, spawn.x, spawn.y, out AttachedSource attached))
        {
            return false;
        }

        attachedSourcesView?.AddViewFor(attached);
        return true;
    }

    public AttachedSourceSaveDto[] CaptureAttachedSourcesSnapshot()
    {
        Initialize();
        if (session == null) return Array.Empty<AttachedSourceSaveDto>();
        return AttachedSourcesSaveMapper.ToSaveDtos(session.AttachedSources);
    }

    public void RestoreAttachedSources(IReadOnlyList<AttachedSource> restoredSources)
    {
        Initialize();
        if (session == null) return;
        session.AttachedSources.ReplaceAll(restoredSources ?? Array.Empty<AttachedSource>());
        attachedSourcesView?.RebuildFromSession();
    }

    private void OnAttachedDetailOpened()
    {
        CancelLink();
        timelineView?.Hide();
        caseTheoryView?.Hide();
        evidenceDetailsPanel.SetActive(false);
        boardPanel.SetActive(false);
        // EB-005: корковата дъска с Player Notes остава видима и активна редом до страничния preview.
        playerNotesView.gameObject.SetActive(true);
    }

    private IReadOnlyList<BoardSourceRecord> GetCaseDocumentSources(string caseId)
    {
        if (string.IsNullOrWhiteSpace(caseId))
        {
            return Array.Empty<BoardSourceRecord>();
        }

        if (caseFilesController == null)
        {
            caseFilesController = FindAnyObjectByType<CaseFilesUIController>(FindObjectsInactive.Include);
        }

        if (caseFilesController == null)
        {
            return Array.Empty<BoardSourceRecord>();
        }

        string trimmedCaseId = caseId.Trim();
        IReadOnlyList<CaseRecordData> records = caseFilesController.GetRecordsForCase(trimmedCaseId);
        if (records == null || records.Count == 0)
        {
            return Array.Empty<BoardSourceRecord>();
        }

        var result = new List<BoardSourceRecord>(records.Count);
        for (int i = 0; i < records.Count; i++)
        {
            CaseRecordData record = records[i];
            if (record == null || string.IsNullOrWhiteSpace(record.SourceId))
            {
                continue;
            }

            result.Add(new BoardSourceRecord(
                record.SourceId,
                AttachedSourceType.Document,
                record.Title,
                trimmedCaseId,
                record.RecordType,
                record.Date,
                record.Body,
                null));
        }

        return result;
    }

    private BoardSourceRecord ResolveCaseDocumentSourceRecord(string caseId, string sourceId)
    {
        if (string.IsNullOrWhiteSpace(caseId) || string.IsNullOrWhiteSpace(sourceId))
        {
            return BoardSourceRecord.CreateMissing(sourceId, AttachedSourceType.Document);
        }

        BoardSourceRecord resolved = ResolveSourceRecord(sourceId, AttachedSourceType.Document);
        if (resolved == null || resolved.IsMissing)
        {
            return BoardSourceRecord.CreateMissing(sourceId, AttachedSourceType.Document);
        }

        if (!string.Equals(resolved.SourceLabel, caseId.Trim(), StringComparison.Ordinal))
        {
            return BoardSourceRecord.CreateMissing(sourceId, AttachedSourceType.Document);
        }

        return resolved;
    }

    private IReadOnlyList<BoardSourceRecord> GetCasePhotoSources(string caseId)
    {
        if (string.IsNullOrWhiteSpace(caseId))
        {
            return Array.Empty<BoardSourceRecord>();
        }

        if (photosController == null)
        {
            photosController = FindAnyObjectByType<PhotosUIController>(FindObjectsInactive.Include);
        }

        if (photosController == null)
        {
            return Array.Empty<BoardSourceRecord>();
        }

        string trimmedCaseId = caseId.Trim();
        IReadOnlyList<PhotoData> casePhotos = photosController.GetPhotosForCase(trimmedCaseId);
        if (casePhotos == null || casePhotos.Count == 0)
        {
            return Array.Empty<BoardSourceRecord>();
        }

        var result = new List<BoardSourceRecord>(casePhotos.Count);
        for (int i = 0; i < casePhotos.Count; i++)
        {
            PhotoData photo = casePhotos[i];
            if (photo == null || string.IsNullOrWhiteSpace(photo.SourceId) || photo.Image == null)
            {
                continue;
            }

            result.Add(new BoardSourceRecord(
                photo.SourceId,
                AttachedSourceType.Photo,
                photo.Title,
                trimmedCaseId,
                photo.Location,
                photo.Date,
                photo.Description,
                photo.Image));
        }

        return result;
    }

    private BoardSourceRecord ResolveCasePhotoSourceRecord(string caseId, string sourceId)
    {
        if (string.IsNullOrWhiteSpace(caseId) || string.IsNullOrWhiteSpace(sourceId))
        {
            return BoardSourceRecord.CreateMissing(sourceId, AttachedSourceType.Photo);
        }

        if (photosController == null)
        {
            photosController = FindAnyObjectByType<PhotosUIController>(FindObjectsInactive.Include);
        }

        string trimmedCaseId = caseId.Trim();
        if (photosController != null
            && photosController.TryFindPhotoForCase(trimmedCaseId, sourceId, out PhotoData photo)
            && photo != null
            && photo.Image != null)
        {
            return new BoardSourceRecord(
                photo.SourceId,
                AttachedSourceType.Photo,
                photo.Title,
                trimmedCaseId,
                photo.Location,
                photo.Date,
                photo.Description,
                photo.Image);
        }

        return BoardSourceRecord.CreateMissing(sourceId, AttachedSourceType.Photo);
    }

    private BoardSourceRecord ResolveSourceRecord(string sourceId, AttachedSourceType sourceType)
    {
        if (sourceType == AttachedSourceType.Document)
        {
            if (caseFilesController == null)
            {
                caseFilesController = FindAnyObjectByType<CaseFilesUIController>(FindObjectsInactive.Include);
            }

            if (caseFilesController != null
                && caseFilesController.TryFindRecordBySourceId(sourceId, out CaseData ownerCase, out CaseRecordData record))
            {
                string sourceLabel = ownerCase != null && !string.IsNullOrWhiteSpace(ownerCase.CaseNumber)
                    ? ownerCase.CaseNumber
                    : "Досиета";
                return new BoardSourceRecord(
                    sourceId,
                    AttachedSourceType.Document,
                    record.Title,
                    sourceLabel,
                    record.RecordType,
                    record.Date,
                    record.Body,
                    null);
            }
        }
        else if (sourceType == AttachedSourceType.Photo)
        {
            if (photosController == null)
            {
                photosController = FindAnyObjectByType<PhotosUIController>(FindObjectsInactive.Include);
            }

            if (photosController != null
                && photosController.TryFindPhotoBySourceId(sourceId, out PhotoData photo))
            {
                return new BoardSourceRecord(
                    sourceId,
                    AttachedSourceType.Photo,
                    photo.Title,
                    "Снимки",
                    photo.Location,
                    photo.Date,
                    photo.Description,
                    photo.Image);
            }
        }

        return BoardSourceRecord.CreateMissing(sourceId, sourceType);
    }

    public PlayerNoteSaveDto[] CapturePlayerNotesSnapshot()
    {
        Initialize();
        if (session == null) return Array.Empty<PlayerNoteSaveDto>();
        playerNotesView?.SyncNotesFromUI();
        return PlayerNotesSaveMapper.ToSaveDtos(session.PlayerNotes);
    }

    public void RestorePlayerNotes(IReadOnlyList<PlayerNote> restoredNotes)
    {
        Initialize();
        if (session == null) return;
        session.PlayerNotes.ReplaceAll(restoredNotes ?? Array.Empty<PlayerNote>());
        playerNotesView?.RebuildFromSession();
    }

    public TimelineEventSaveDto[] CaptureTimelineSnapshot()
    {
        Initialize();
        if (session == null) return Array.Empty<TimelineEventSaveDto>();
        return TimelineSaveMapper.ToSaveDtos(session.Timeline);
    }

    public void RestoreTimeline(IReadOnlyList<TimelineEvent> restoredEvents)
    {
        Initialize();
        if (session == null) return;
        session.Timeline.ReplaceAll(restoredEvents ?? Array.Empty<TimelineEvent>());
        timelineView?.RebuildFromSession();
    }

    public CaseTheorySaveDto[] CaptureCaseTheoriesSnapshot()
    {
        Initialize();
        if (session == null) return Array.Empty<CaseTheorySaveDto>();
        caseTheoryView?.SyncFromUI();
        return CaseTheorySaveMapper.ToSaveDtos(session.CaseTheories);
    }

    public void RestoreCaseTheories(IReadOnlyList<CaseTheoryDraft> restoredTheories)
    {
        Initialize();
        if (session == null) return;
        session.CaseTheories.ReplaceAll(restoredTheories ?? Array.Empty<CaseTheoryDraft>());
        caseTheoryView?.RefreshFromSession();
    }

    private void BuildModeTabs()
    {
        Transform root = boardRoot.transform;
        boardModeTabButton = Button(root, "BoardModeTabButton", "ДЪСКА",
            new Vector2(.03f, .914f), new Vector2(.175f, .972f),
            BackToBoard, EvidenceBoardVisuals.ButtonStyle.PrimaryManila);
        boardModeTabButton.navigation = new Navigation { mode = Navigation.Mode.None };

        timelineModeTabButton = Button(root, "TimelineModeTabButton", "ВРЕМЕВА ЛИНИЯ",
            new Vector2(.185f, .914f), new Vector2(.385f, .972f),
            ShowTimeline, EvidenceBoardVisuals.ButtonStyle.CorkControl);
        timelineModeTabButton.navigation = new Navigation { mode = Navigation.Mode.None };

        theoryModeTabButton = Button(root, "TheoryModeTabButton", "МОЯТА ТЕОРИЯ",
            new Vector2(.395f, .914f), new Vector2(.595f, .972f),
            ShowTheory, EvidenceBoardVisuals.ButtonStyle.CorkControl);
        theoryModeTabButton.navigation = new Navigation { mode = Navigation.Mode.None };

        UpdateModeTabVisuals(BoardViewMode.Board);
    }

    private void UpdateModeTabVisuals(BoardViewMode activeMode)
    {
        if (boardModeTabButton != null)
        {
            EvidenceBoardVisuals.ApplyButtonStyle(
                boardModeTabButton,
                (Image)boardModeTabButton.targetGraphic,
                boardModeTabButton.GetComponentInChildren<TMP_Text>(),
                activeMode == BoardViewMode.Board ? EvidenceBoardVisuals.ButtonStyle.PrimaryManila : EvidenceBoardVisuals.ButtonStyle.CorkControl);
        }

        if (timelineModeTabButton != null)
        {
            EvidenceBoardVisuals.ApplyButtonStyle(
                timelineModeTabButton,
                (Image)timelineModeTabButton.targetGraphic,
                timelineModeTabButton.GetComponentInChildren<TMP_Text>(),
                activeMode == BoardViewMode.Timeline ? EvidenceBoardVisuals.ButtonStyle.PrimaryManila : EvidenceBoardVisuals.ButtonStyle.CorkControl);
        }

        if (theoryModeTabButton != null)
        {
            EvidenceBoardVisuals.ApplyButtonStyle(
                theoryModeTabButton,
                (Image)theoryModeTabButton.targetGraphic,
                theoryModeTabButton.GetComponentInChildren<TMP_Text>(),
                activeMode == BoardViewMode.Theory ? EvidenceBoardVisuals.ButtonStyle.PrimaryManila : EvidenceBoardVisuals.ButtonStyle.CorkControl);
        }
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
            cardProvenanceTexts[i].fontSize = 22;
            cardProvenanceTexts[i].text = $"{evidenceCards[i].Type.ToUpperInvariant()} — {evidenceCards[i].Source}";
        }

        // Визуална връзка между десния панел с доказателства и лявата коркова дъска.
        Outline panelOutline = boardPanel.GetComponent<Outline>();
        if (panelOutline == null) panelOutline = boardPanel.AddComponent<Outline>();
        panelOutline.effectColor = EvidenceBoardVisuals.BrassHairline;
        panelOutline.effectDistance = new Vector2(1.5f, -1.5f);
        RectTransform leftTrim = Rect(panel, "BoardPanelLeftTrim", new Vector2(0f, .01f), new Vector2(.008f, .99f));
        Image leftTrimImg = leftTrim.gameObject.AddComponent<Image>();
        leftTrimImg.color = EvidenceBoardVisuals.BrassAccent;
        leftTrimImg.raycastTarget = false;

        linksLayer = Rect(panel, "BoardLinks", Vector2.zero, Vector2.one);
        linksLayer.SetAsFirstSibling();
        Button linkButton = Button(panel, "LinkCardsButton", "СВЪРЖИ", new Vector2(.06f, .38f),
            new Vector2(.45f, .44f), BeginLink, EvidenceBoardVisuals.ButtonStyle.CaseAction);
        cancelLinkButton = Button(panel, "CancelLinkButton", "ОТКАЖИ", new Vector2(.55f, .38f),
            new Vector2(.94f, .44f), CancelLink, EvidenceBoardVisuals.ButtonStyle.CaseMutedDanger);
        statusText = Label(panel, "BoardLinkStatus", "", new Vector2(.06f, .34f),
            new Vector2(.94f, .38f), 18, TextAlignmentOptions.Center);
        statusText.color = EvidenceBoardVisuals.StatusTextColor;
        removeButtons = new Button[LinkPairs.Length];
        removeLabels = new TMP_Text[LinkPairs.Length];
        for (int i = 0; i < LinkPairs.Length; i++)
        {
            Vector2Int pair = LinkPairs[i];
            float top = .34f - i * .05f;
            removeButtons[i] = Button(panel, "RemoveLink" + i, "", new Vector2(.06f, top - .05f),
                new Vector2(.94f, top), () => RemoveLink(pair.x, pair.y), EvidenceBoardVisuals.ButtonStyle.CaseMutedDanger);
            removeLabels[i] = removeButtons[i].GetComponentInChildren<TMP_Text>();
            removeLabels[i].fontSize = 18;
        }

        // Отделена, ясно обозначена наследена зона за старото общо поле за бележки.
        RectTransform legacyDivider = Rect(panel, "LegacyNotesDivider", new Vector2(.055f, .186f), new Vector2(.945f, .189f));
        Image legacyDividerImg = legacyDivider.gameObject.AddComponent<Image>();
        legacyDividerImg.color = EvidenceBoardVisuals.BrassHairline;
        legacyDividerImg.raycastTarget = false;

        RectTransform legacyFrame = Rect(panel, "LegacyNotesContainer", new Vector2(.055f, .016f), new Vector2(.945f, .184f));
        Image legacyFrameImg = legacyFrame.gameObject.AddComponent<Image>();
        legacyFrameImg.sprite = EvidenceBoardVisuals.ButtonSprite;
        legacyFrameImg.type = Image.Type.Sliced;
        legacyFrameImg.color = new Color(.06f, .08f, .085f, .78f);
        legacyFrameImg.raycastTarget = false;

        TMP_Text legacyHeader = Label(panel, "BoardNotesHeader", "ОБЩА БЕЛЕЖКА (НАСЛЕДЕНА) — само за тази сесия",
            new Vector2(.068f, .142f), new Vector2(.932f, .180f), 15, TextAlignmentOptions.MidlineLeft);
        legacyHeader.color = EvidenceBoardVisuals.MutedLabelText;
        BuildNotesInput(panel);
        detailProvenanceText = Label((RectTransform)evidenceDetailsPanel.transform, "BoardDetailSource", "",
            new Vector2(.07f, .19f), new Vector2(.93f, .30f), 20, TextAlignmentOptions.Left);
        cancelLinkButton.gameObject.SetActive(false);
        linkButton.onClick.AddListener(() => cancelLinkButton.gameObject.SetActive(linking));
    }

    private void BuildNotesInput(RectTransform panel)
    {
        RectTransform rect = Rect(panel, "BoardNotesInput", new Vector2(.068f, .024f), new Vector2(.932f, .138f));
        Image bg = rect.gameObject.AddComponent<Image>();
        bg.sprite = EvidenceBoardVisuals.ButtonSprite;
        bg.type = Image.Type.Sliced;
        bg.color = new Color(.10f, .12f, .12f, .96f);
        rect.gameObject.AddComponent<RectMask2D>();
        TMP_Text value = Label(rect, "Text", "", new Vector2(.025f, .08f), new Vector2(.975f, .92f),
            16, TextAlignmentOptions.TopLeft);
        value.textWrappingMode = TextWrappingModes.Normal;
        TMP_Text placeholder = Label(rect, "Placeholder", "Старо общо поле — за отделни листа използвайте дъската вляво",
            new Vector2(.025f, .08f), new Vector2(.975f, .92f), 16, TextAlignmentOptions.TopLeft);
        placeholder.color = new Color(.60f, .63f, .59f, .80f);
        placeholder.fontStyle = FontStyles.Italic;
        placeholder.textWrappingMode = TextWrappingModes.Normal;
        notesInput = rect.gameObject.AddComponent<TMP_InputField>();
        notesInput.targetGraphic = bg;
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
            removeLabels[i].text = $"{evidenceCards[pair.x].Type} ↔ {evidenceCards[pair.y].Type} — Премахни";
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
        UnityEngine.Events.UnityAction click, EvidenceBoardVisuals.ButtonStyle style)
    {
        RectTransform rect = Rect(parent, name, min, max);
        Image image = rect.gameObject.AddComponent<Image>();
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(click);
        TMP_Text label = Label(rect, "Label", value, Vector2.zero, Vector2.one, 18, TextAlignmentOptions.Center);
        EvidenceBoardVisuals.ApplyButtonStyle(button, image, label, style);
        return button;
    }
}
