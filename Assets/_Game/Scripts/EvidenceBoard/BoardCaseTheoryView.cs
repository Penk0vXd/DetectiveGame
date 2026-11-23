using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Управлява екрана „МОЯТА ТЕОРИЯ — РАБОТНА ЧЕРНОВА“ в Evidence Board (EB-013, EB-014, EB-015).
// Представя 6 свободно редактируеми текстови полета, секция „ИЗБРАНИ ИЗТОЧНИЦИ“ и read-only преглед на избраните документи без проверка за правилност и без официално подаване.
public sealed class BoardCaseTheoryView : MonoBehaviour
{
    private const int ShortFieldLimit = 300;
    private const int MediumFieldLimit = 1500;
    private const int LongFieldLimit = 3000;
    private const string MissingSourceLabel = "Недостъпен източник";
    private const string MissingSourcePreviewUnavailableLabel = "Документът не е наличен за преглед.";
    private const string MissingPhotoLabel = "Недостъпна снимка";
    private const string MissingPhotoPreviewUnavailableLabel = "СНИМКА • Снимката не е налична за преглед.";
    private const string EmptyDocumentsSelectorLabel = "Няма налични документи за това дело.";
    private const string EmptyPhotosSelectorLabel = "Няма налични снимки за това дело.";

    private CaseTheorySession session;
    private string activeCaseId = CaseTheorySession.DefaultCaseId;
    private TMP_FontAsset font;
    private Func<string, IReadOnlyList<BoardSourceRecord>> availableSourcesProvider;
    private Func<string, BoardSourceRecord> documentSourceResolver;
    private Func<string, IReadOnlyList<BoardSourceRecord>> availablePhotoSourcesProvider;
    private Func<string, BoardSourceRecord> photoSourceResolver;

    private ScrollRect formScroll;
    private RectTransform formContent;

    private TMP_InputField mainSuspectInput;
    private TMP_InputField participantsInput;
    private TMP_InputField motiveInput;
    private TMP_InputField reconstructionInput;
    private TMP_InputField supportingEvidenceInput;
    private TMP_InputField unresolvedQuestionsInput;

    private RectTransform referencedSourcesList;
    private GameObject referencedSourcesEmptyLabel;
    private readonly List<GameObject> referencedSourceRows = new List<GameObject>();

    private GameObject sourceSelectorOverlay;
    private RectTransform sourceSelectorContent;
    private GameObject sourceSelectorEmptyLabel;
    private TMP_Text sourceSelectorEmptyText;
    private ScrollRect sourceSelectorScroll;
    private Button selectorDocumentsTabButton;
    private Button selectorPhotosTabButton;
    private AttachedSourceType activeSelectorCategory = AttachedSourceType.Document;
    private readonly List<GameObject> selectorItemRows = new List<GameObject>();

    private GameObject sourcePreviewOverlay;
    private BoardSourcePreviewView sourcePreviewView;
    private float savedScrollBeforePreview = 1f;
    private bool hasSavedScrollBeforePreview;

    public bool IsOpen => gameObject.activeSelf;
    public bool IsSelectorOpen => sourceSelectorOverlay != null && sourceSelectorOverlay.activeSelf;
    public bool IsPreviewOpen => sourcePreviewOverlay != null && sourcePreviewOverlay.activeSelf;
    public AttachedSourceType ActiveSelectorCategory => activeSelectorCategory;
    public string ActiveCaseId => activeCaseId;

    public static BoardCaseTheoryView Create(
        Transform boardRoot,
        CaseTheorySession session,
        TMP_FontAsset font,
        Func<string, IReadOnlyList<BoardSourceRecord>> availableSourcesProvider = null,
        Func<string, BoardSourceRecord> documentSourceResolver = null,
        string caseId = CaseTheorySession.DefaultCaseId,
        Func<string, IReadOnlyList<BoardSourceRecord>> availablePhotoSourcesProvider = null,
        Func<string, BoardSourceRecord> photoSourceResolver = null)
    {
        RectTransform frame = PlayerNoteView.Rect(boardRoot, "CaseTheoryPanel",
            new Vector2(.03f, .13f), new Vector2(.96f, .90f));
        Image frameImg = frame.gameObject.AddComponent<Image>();
        frameImg.sprite = EvidenceBoardVisuals.WoodFrameSprite;
        frameImg.type = Image.Type.Sliced;
        frameImg.color = Color.white;
        frameImg.raycastTarget = true;

        Outline frameOutline = frame.gameObject.AddComponent<Outline>();
        frameOutline.effectColor = EvidenceBoardVisuals.BrassHairline;
        frameOutline.effectDistance = new Vector2(1.5f, -1.5f);

        Shadow frameShadow = frame.gameObject.AddComponent<Shadow>();
        frameShadow.effectColor = new Color(0f, 0f, 0f, 0.48f);
        frameShadow.effectDistance = new Vector2(0f, -6f);

        BoardCaseTheoryView view = frame.gameObject.AddComponent<BoardCaseTheoryView>();
        view.session = session;
        view.font = font;
        view.availableSourcesProvider = availableSourcesProvider;
        view.documentSourceResolver = documentSourceResolver;
        view.availablePhotoSourcesProvider = availablePhotoSourcesProvider;
        view.photoSourceResolver = photoSourceResolver;
        view.activeCaseId = string.IsNullOrWhiteSpace(caseId) ? CaseTheorySession.DefaultCaseId : caseId.Trim();
        view.Build(frame);
        frame.gameObject.SetActive(false);
        return view;
    }

    public void Show()
    {
        CloseSourceSelector();
        CloseSourcePreview();
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        RefreshFromSession();
    }

    public void Hide()
    {
        CloseSourceSelector();
        CloseSourcePreview();
        SyncFromUI();
        if (gameObject.activeSelf)
            gameObject.SetActive(false);
    }

    public void OpenSourceSelector()
    {
        if (sourceSelectorOverlay == null)
            return;

        CloseSourcePreview();
        SyncFromUI();
        activeSelectorCategory = AttachedSourceType.Document;
        UpdateSelectorCategoryVisuals();
        RebuildSourceSelectorItems();
        sourceSelectorOverlay.SetActive(true);
        sourceSelectorOverlay.transform.SetAsLastSibling();
        if (sourceSelectorScroll != null)
            sourceSelectorScroll.verticalNormalizedPosition = 1f;
        RebuildLayout(sourceSelectorContent);
    }

    public void SelectSourceSelectorCategory(AttachedSourceType category)
    {
        activeSelectorCategory = category;
        UpdateSelectorCategoryVisuals();
        RebuildSourceSelectorItems();
        if (sourceSelectorScroll != null)
            sourceSelectorScroll.verticalNormalizedPosition = 1f;
    }

    public void CloseSourceSelector()
    {
        if (sourceSelectorOverlay != null && sourceSelectorOverlay.activeSelf)
            sourceSelectorOverlay.SetActive(false);
    }

    public bool OpenSourcePreview(string sourceId) =>
        OpenSourcePreview(AttachedSourceType.Document, sourceId);

    public bool OpenSourcePreview(AttachedSourceType sourceType, string sourceId)
    {
        if (sourcePreviewOverlay == null || sourcePreviewView == null)
            return false;

        SyncFromUI();
        DeactivateActiveTheoryInputs();

        BoardSourceRecord resolved = ResolveSourceForActiveCase(sourceType, sourceId);
        if (resolved == null || resolved.IsMissing || (sourceType == AttachedSourceType.Photo && resolved.Image == null))
        {
            CloseSourcePreview();
            return false;
        }

        CloseSourceSelector();
        if (!IsPreviewOpen && formScroll != null)
        {
            savedScrollBeforePreview = formScroll.verticalNormalizedPosition;
            hasSavedScrollBeforePreview = true;
        }

        sourcePreviewOverlay.SetActive(true);
        sourcePreviewOverlay.transform.SetAsLastSibling();
        sourcePreviewView.Show(sourceType, resolved);
        return true;
    }

    public void CloseSourcePreview()
    {
        if (sourcePreviewView != null)
            sourcePreviewView.Hide();

        if (sourcePreviewOverlay != null && sourcePreviewOverlay.activeSelf)
            sourcePreviewOverlay.SetActive(false);

        if (hasSavedScrollBeforePreview && formScroll != null)
        {
            formScroll.verticalNormalizedPosition = savedScrollBeforePreview;
            hasSavedScrollBeforePreview = false;
        }

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    public void SyncFromUI()
    {
        if (session == null || mainSuspectInput == null)
            return;

        bool anyNonEmpty =
            !string.IsNullOrEmpty(mainSuspectInput.text)
            || !string.IsNullOrEmpty(participantsInput.text)
            || !string.IsNullOrEmpty(motiveInput.text)
            || !string.IsNullOrEmpty(reconstructionInput.text)
            || !string.IsNullOrEmpty(supportingEvidenceInput.text)
            || !string.IsNullOrEmpty(unresolvedQuestionsInput.text);

        if (!anyNonEmpty && !session.TryGetDraft(activeCaseId, out _))
            return;

        CaseTheoryDraft draft = session.GetOrCreateDraft(activeCaseId);
        draft.SetMainSuspect(mainSuspectInput.text);
        draft.SetParticipantsAndRoles(participantsInput.text);
        draft.SetMotive(motiveInput.text);
        draft.SetReconstruction(reconstructionInput.text);
        draft.SetSupportingEvidence(supportingEvidenceInput.text);
        draft.SetUnresolvedQuestions(unresolvedQuestionsInput.text);
    }

    public void RefreshFromSession()
    {
        if (mainSuspectInput == null)
            return;

        CaseTheoryDraft draft = null;
        session?.TryGetDraft(activeCaseId, out draft);

        mainSuspectInput.SetTextWithoutNotify(draft != null ? draft.MainSuspect : string.Empty);
        participantsInput.SetTextWithoutNotify(draft != null ? draft.ParticipantsAndRoles : string.Empty);
        motiveInput.SetTextWithoutNotify(draft != null ? draft.Motive : string.Empty);
        reconstructionInput.SetTextWithoutNotify(draft != null ? draft.Reconstruction : string.Empty);
        supportingEvidenceInput.SetTextWithoutNotify(draft != null ? draft.SupportingEvidence : string.Empty);
        unresolvedQuestionsInput.SetTextWithoutNotify(draft != null ? draft.UnresolvedQuestions : string.Empty);

        RefreshReferencedSourcesList(draft);
        if (IsSelectorOpen)
            RebuildSourceSelectorItems();
        RebuildLayout(formContent);
    }

    private void Build(RectTransform frame)
    {
        RectTransform inner = PlayerNoteView.Rect(frame, "InnerSurface", new Vector2(.010f, .014f), new Vector2(.990f, .986f));
        Image innerImg = inner.gameObject.AddComponent<Image>();
        innerImg.color = new Color(0.115f, 0.095f, 0.080f, 0.98f);
        innerImg.raycastTarget = true;

        // Горна лайсна със заглавие, пояснение и номер на делото.
        RectTransform headerRail = PlayerNoteView.Rect(inner, "TheoryHeaderRail", new Vector2(0f, .875f), new Vector2(1f, 1f));
        Image headerRailImg = headerRail.gameObject.AddComponent<Image>();
        headerRailImg.color = EvidenceBoardVisuals.HeaderRailColor;
        headerRailImg.raycastTarget = false;

        RectTransform headerAccent = PlayerNoteView.Rect(inner, "TheoryHeaderAccent", new Vector2(0f, .872f), new Vector2(1f, .876f));
        Image headerAccentImg = headerAccent.gameObject.AddComponent<Image>();
        headerAccentImg.color = EvidenceBoardVisuals.BrassHairline;
        headerAccentImg.raycastTarget = false;

        RectTransform titleRect = PlayerNoteView.Rect(inner, "TheoryHeaderTitle", new Vector2(.022f, .930f), new Vector2(.76f, .992f));
        TMP_Text titleText = PlayerNoteView.Label(titleRect, "Text", "МОЯТА ТЕОРИЯ — РАБОТНА ЧЕРНОВА", font, 21f, TextAlignmentOptions.MidlineLeft);
        titleText.color = EvidenceBoardVisuals.ParchmentText;
        titleText.fontStyle = FontStyles.Bold;
        titleText.textWrappingMode = TextWrappingModes.NoWrap;
        titleText.overflowMode = TextOverflowModes.Ellipsis;

        RectTransform subtitleRect = PlayerNoteView.Rect(inner, "TheoryHeaderSubtitle", new Vector2(.022f, .880f), new Vector2(.76f, .932f));
        TMP_Text subtitleText = PlayerNoteView.Label(subtitleRect, "Text",
            "Запиши собственото си обяснение на случая. Можеш да го променяш по всяко време.",
            font, 15f, TextAlignmentOptions.MidlineLeft);
        subtitleText.color = EvidenceBoardVisuals.MutedLabelText;
        subtitleText.textWrappingMode = TextWrappingModes.NoWrap;
        subtitleText.overflowMode = TextOverflowModes.Ellipsis;

        RectTransform caseBadgeRect = PlayerNoteView.Rect(inner, "TheoryCaseBadge", new Vector2(.77f, .895f), new Vector2(.978f, .978f));
        TMP_Text caseBadgeText = PlayerNoteView.Label(caseBadgeRect, "Text", "ДЕЛО: " + activeCaseId, font, 16f, TextAlignmentOptions.MidlineRight);
        caseBadgeText.color = EvidenceBoardVisuals.StatusTextColor;
        caseBadgeText.fontStyle = FontStyles.Bold;
        caseBadgeText.textWrappingMode = TextWrappingModes.NoWrap;
        caseBadgeText.overflowMode = TextOverflowModes.Ellipsis;

        // Скролируем канцеларски работен лист с 6-те полета.
        RectTransform scrollFrame = PlayerNoteView.Rect(inner, "TheoryFormScrollView", new Vector2(.022f, .022f), new Vector2(.978f, .856f));
        Image scrollFrameBg = scrollFrame.gameObject.AddComponent<Image>();
        scrollFrameBg.sprite = EvidenceBoardVisuals.PrintoutPaperSprite;
        scrollFrameBg.color = new Color(0.95f, 0.92f, 0.84f, 1f);
        scrollFrameBg.raycastTarget = true;

        Outline scrollOutline = scrollFrame.gameObject.AddComponent<Outline>();
        scrollOutline.effectColor = EvidenceBoardVisuals.BrassHairline;
        scrollOutline.effectDistance = new Vector2(1.5f, -1.5f);

        RectTransform viewport = PlayerNoteView.Rect(scrollFrame, "Viewport", new Vector2(.018f, .022f), new Vector2(.982f, .978f));
        viewport.gameObject.AddComponent<RectMask2D>();

        formContent = PlayerNoteView.Rect(viewport, "Content", new Vector2(0f, 1f), new Vector2(1f, 1f));
        formContent.pivot = new Vector2(0.5f, 1f);

        VerticalLayoutGroup layout = formContent.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(22, 22, 16, 26);
        layout.spacing = 14f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = formContent.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        formScroll = scrollFrame.gameObject.AddComponent<ScrollRect>();
        formScroll.horizontal = false;
        formScroll.vertical = true;
        formScroll.viewport = viewport;
        formScroll.content = formContent;
        formScroll.movementType = ScrollRect.MovementType.Clamped;
        formScroll.inertia = true;
        formScroll.decelerationRate = 0.135f;
        formScroll.scrollSensitivity = 32f;

        mainSuspectInput = CreateField(
            "TheoryMainSuspect",
            "1. ОСНОВЕН ЗАПОДОЗРЯН",
            "Въведи име или свободно описание на лицето, което подозираш...",
            52f,
            multiline: false,
            ShortFieldLimit,
            value => session?.GetOrCreateDraft(activeCaseId).SetMainSuspect(value));

        participantsInput = CreateField(
            "TheoryParticipants",
            "2. ДРУГИ УЧАСТНИЦИ И ВЪЗМОЖНИ РОЛИ",
            "Опиши евентуални други участници и каква роля може да е имал всеки от тях...",
            106f,
            multiline: true,
            MediumFieldLimit,
            value => session?.GetOrCreateDraft(activeCaseId).SetParticipantsAndRoles(value));

        motiveInput = CreateField(
            "TheoryMotive",
            "3. ПРЕДПОЛАГАЕМ МОТИВ",
            "Защо според теб е извършена кражбата на Project Orion...",
            98f,
            multiline: true,
            MediumFieldLimit,
            value => session?.GetOrCreateDraft(activeCaseId).SetMotive(value));

        reconstructionInput = CreateField(
            "TheoryReconstruction",
            "4. МОЯТА РЕКОНСТРУКЦИЯ",
            "Опиши със собствени думи как смяташ, че са се развили събитията...",
            165f,
            multiline: true,
            LongFieldLimit,
            value => session?.GetOrCreateDraft(activeCaseId).SetReconstruction(value));

        supportingEvidenceInput = CreateField(
            "TheorySupportingEvidence",
            "5. ПОДКРЕПЯЩИ ДОКАЗАТЕЛСТВА",
            "Запиши кои документи, часове или записи според теб подкрепят тази теория...",
            116f,
            multiline: true,
            MediumFieldLimit,
            value => session?.GetOrCreateDraft(activeCaseId).SetSupportingEvidence(value));

        unresolvedQuestionsInput = CreateField(
            "TheoryUnresolvedQuestions",
            "6. НЕРЕШЕНИ ВЪПРОСИ",
            "Какво все още не можеш да обясниш или искаш да провериш допълнително...",
            106f,
            multiline: true,
            MediumFieldLimit,
            value => session?.GetOrCreateDraft(activeCaseId).SetUnresolvedQuestions(value));

        BuildReferencedSourcesSection();
        BuildSourceSelectorOverlay(frame);
        BuildSourcePreviewOverlay(frame);
    }

    private void BuildReferencedSourcesSection()
    {
        // Разделителна линия между 6-те текстови полета и секцията „ИЗБРАНИ ИЗТОЧНИЦИ“.
        var dividerGo = new GameObject("TheoryReferencesDivider", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
        RectTransform dividerRect = dividerGo.GetComponent<RectTransform>();
        dividerRect.SetParent(formContent, false);
        dividerRect.localScale = Vector3.one;
        LayoutElement dividerLayout = dividerGo.GetComponent<LayoutElement>();
        dividerLayout.minHeight = 2f;
        dividerLayout.preferredHeight = 2f;
        Image dividerImg = dividerGo.GetComponent<Image>();
        dividerImg.color = new Color(0.45f, 0.37f, 0.25f, 0.45f);
        dividerImg.raycastTarget = false;

        var sectionGo = new GameObject("TheoryReferencedSourcesSection", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        RectTransform sectionRect = sectionGo.GetComponent<RectTransform>();
        sectionRect.SetParent(formContent, false);
        sectionRect.localScale = Vector3.one;

        VerticalLayoutGroup sectionLayout = sectionGo.GetComponent<VerticalLayoutGroup>();
        sectionLayout.padding = new RectOffset(0, 0, 4, 4);
        sectionLayout.spacing = 8f;
        sectionLayout.childAlignment = TextAnchor.UpperLeft;
        sectionLayout.childControlWidth = true;
        sectionLayout.childControlHeight = true;
        sectionLayout.childForceExpandWidth = true;
        sectionLayout.childForceExpandHeight = false;

        // Заглавен ред със заглавие и бутон „ДОБАВИ ИЗТОЧНИК“.
        var headerRowGo = new GameObject("TheoryReferencesHeaderRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        RectTransform headerRowRect = headerRowGo.GetComponent<RectTransform>();
        headerRowRect.SetParent(sectionRect, false);
        headerRowRect.localScale = Vector3.one;
        LayoutElement headerRowLayoutElem = headerRowGo.GetComponent<LayoutElement>();
        headerRowLayoutElem.minHeight = 36f;
        headerRowLayoutElem.preferredHeight = 38f;

        HorizontalLayoutGroup headerRowLayout = headerRowGo.GetComponent<HorizontalLayoutGroup>();
        headerRowLayout.padding = new RectOffset(0, 0, 0, 0);
        headerRowLayout.spacing = 12f;
        headerRowLayout.childAlignment = TextAnchor.MiddleLeft;
        headerRowLayout.childControlWidth = true;
        headerRowLayout.childControlHeight = true;
        headerRowLayout.childForceExpandWidth = false;
        headerRowLayout.childForceExpandHeight = false;

        var titleGo = new GameObject("TheoryReferencesTitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
        RectTransform titleRect = titleGo.GetComponent<RectTransform>();
        titleRect.SetParent(headerRowRect, false);
        titleRect.localScale = Vector3.one;
        LayoutElement titleLayout = titleGo.GetComponent<LayoutElement>();
        titleLayout.flexibleWidth = 1f;
        titleLayout.minHeight = 30f;
        titleLayout.preferredHeight = 34f;

        TMP_Text titleText = titleGo.GetComponent<TMP_Text>();
        titleText.font = font;
        titleText.fontSize = 16.5f;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.MidlineLeft;
        titleText.color = new Color(0.24f, 0.18f, 0.12f, 1f);
        titleText.raycastTarget = false;
        titleText.text = "ИЗБРАНИ ИЗТОЧНИЦИ";

        Button addSourceButton = CreateActionButton(
            headerRowRect,
            "AddTheorySourceButton",
            "ДОБАВИ ИЗТОЧНИК",
            210f,
            34f,
            15f,
            OpenSourceSelector,
            EvidenceBoardVisuals.ButtonStyle.CaseAction);
        addSourceButton.navigation = new Navigation { mode = Navigation.Mode.None };

        // Пояснителен текст (неутрален, без оценка).
        var hintGo = new GameObject("TheoryReferencesHint", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
        RectTransform hintRect = hintGo.GetComponent<RectTransform>();
        hintRect.SetParent(sectionRect, false);
        hintRect.localScale = Vector3.one;
        LayoutElement hintLayout = hintGo.GetComponent<LayoutElement>();
        hintLayout.minHeight = 22f;
        hintLayout.flexibleWidth = 1f;

        TMP_Text hintText = hintGo.GetComponent<TMP_Text>();
        hintText.font = font;
        hintText.fontSize = 14.5f;
        hintText.fontStyle = FontStyles.Italic;
        hintText.alignment = TextAlignmentOptions.TopLeft;
        hintText.color = new Color(0.36f, 0.30f, 0.22f, 0.92f);
        hintText.textWrappingMode = TextWrappingModes.Normal;
        hintText.raycastTarget = false;
        hintText.text = "Документи, които си избрал да използваш в теорията си. Изборът им не означава, че заключението ти е правилно.";

        // Списък с ръчно избрани източници.
        var listGo = new GameObject("TheoryReferencedSourcesList", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        referencedSourcesList = listGo.GetComponent<RectTransform>();
        referencedSourcesList.SetParent(sectionRect, false);
        referencedSourcesList.localScale = Vector3.one;

        VerticalLayoutGroup listLayout = listGo.GetComponent<VerticalLayoutGroup>();
        listLayout.padding = new RectOffset(0, 0, 2, 0);
        listLayout.spacing = 6f;
        listLayout.childAlignment = TextAnchor.UpperLeft;
        listLayout.childControlWidth = true;
        listLayout.childControlHeight = true;
        listLayout.childForceExpandWidth = true;
        listLayout.childForceExpandHeight = false;

        var emptyGo = new GameObject("TheoryReferencedSourcesEmpty", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
        RectTransform emptyRect = emptyGo.GetComponent<RectTransform>();
        emptyRect.SetParent(referencedSourcesList, false);
        emptyRect.localScale = Vector3.one;
        LayoutElement emptyLayout = emptyGo.GetComponent<LayoutElement>();
        emptyLayout.minHeight = 28f;
        emptyLayout.preferredHeight = 30f;

        TMP_Text emptyText = emptyGo.GetComponent<TMP_Text>();
        emptyText.font = font;
        emptyText.fontSize = 15f;
        emptyText.fontStyle = FontStyles.Italic;
        emptyText.alignment = TextAlignmentOptions.MidlineLeft;
        emptyText.color = EvidenceBoardVisuals.PaperPlaceholder;
        emptyText.raycastTarget = false;
        emptyText.text = "Няма избрани източници.";
        referencedSourcesEmptyLabel = emptyGo;
    }

    private void BuildSourceSelectorOverlay(RectTransform frame)
    {
        // Модален слой върху CaseTheoryPanel — блокира кликове към полетата и бутоните отзад.
        RectTransform overlayRect = PlayerNoteView.Rect(frame, "TheorySourceSelectorOverlay", Vector2.zero, Vector2.one);
        Image backdrop = overlayRect.gameObject.AddComponent<Image>();
        backdrop.color = new Color(0.05f, 0.04f, 0.03f, 0.74f);
        backdrop.raycastTarget = true;

        RectTransform dialogRect = PlayerNoteView.Rect(overlayRect, "TheorySourceSelectorDialog",
            new Vector2(.08f, .06f), new Vector2(.92f, .94f));
        Image dialogFrameImg = dialogRect.gameObject.AddComponent<Image>();
        dialogFrameImg.sprite = EvidenceBoardVisuals.WoodFrameSprite;
        dialogFrameImg.type = Image.Type.Sliced;
        dialogFrameImg.color = Color.white;
        dialogFrameImg.raycastTarget = true;

        Outline dialogOutline = dialogRect.gameObject.AddComponent<Outline>();
        dialogOutline.effectColor = EvidenceBoardVisuals.BrassHairline;
        dialogOutline.effectDistance = new Vector2(1.5f, -1.5f);

        Shadow dialogShadow = dialogRect.gameObject.AddComponent<Shadow>();
        dialogShadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
        dialogShadow.effectDistance = new Vector2(0f, -6f);

        RectTransform dialogInner = PlayerNoteView.Rect(dialogRect, "InnerSurface",
            new Vector2(.014f, .018f), new Vector2(.986f, .982f));
        Image dialogInnerImg = dialogInner.gameObject.AddComponent<Image>();
        dialogInnerImg.color = new Color(0.115f, 0.095f, 0.080f, 0.99f);
        dialogInnerImg.raycastTarget = true;

        // Горна лента на selector-а.
        RectTransform headerRail = PlayerNoteView.Rect(dialogInner, "SelectorHeaderRail",
            new Vector2(0f, .86f), new Vector2(1f, 1f));
        Image headerRailImg = headerRail.gameObject.AddComponent<Image>();
        headerRailImg.color = EvidenceBoardVisuals.HeaderRailColor;
        headerRailImg.raycastTarget = false;

        RectTransform headerAccent = PlayerNoteView.Rect(dialogInner, "SelectorHeaderAccent",
            new Vector2(0f, .856f), new Vector2(1f, .860f));
        Image headerAccentImg = headerAccent.gameObject.AddComponent<Image>();
        headerAccentImg.color = EvidenceBoardVisuals.BrassHairline;
        headerAccentImg.raycastTarget = false;

        RectTransform titleRect = PlayerNoteView.Rect(dialogInner, "SelectorTitle",
            new Vector2(.025f, .922f), new Vector2(.74f, .990f));
        TMP_Text titleText = PlayerNoteView.Label(titleRect, "Text", "ИЗБОР НА ИЗТОЧНИК", font, 19f, TextAlignmentOptions.MidlineLeft);
        titleText.color = EvidenceBoardVisuals.ParchmentText;
        titleText.fontStyle = FontStyles.Bold;
        titleText.textWrappingMode = TextWrappingModes.NoWrap;
        titleText.overflowMode = TextOverflowModes.Ellipsis;

        RectTransform subtitleRect = PlayerNoteView.Rect(dialogInner, "SelectorSubtitle",
            new Vector2(.025f, .868f), new Vector2(.74f, .924f));
        TMP_Text subtitleText = PlayerNoteView.Label(subtitleRect, "Text",
            "ДЕЛО: " + activeCaseId + " — избери документ или снимка, за да добавиш към теорията си.",
            font, 14.5f, TextAlignmentOptions.MidlineLeft);
        subtitleText.color = EvidenceBoardVisuals.MutedLabelText;
        subtitleText.textWrappingMode = TextWrappingModes.NoWrap;
        subtitleText.overflowMode = TextOverflowModes.Ellipsis;

        RectTransform closeBtnRect = PlayerNoteView.Rect(dialogInner, "CloseTheorySourceSelectorButton",
            new Vector2(.78f, .885f), new Vector2(.975f, .972f));
        Image closeBtnImg = closeBtnRect.gameObject.AddComponent<Image>();
        Button closeBtn = closeBtnRect.gameObject.AddComponent<Button>();
        closeBtn.targetGraphic = closeBtnImg;
        closeBtn.navigation = new Navigation { mode = Navigation.Mode.None };
        closeBtn.onClick.AddListener(CloseSourceSelector);
        TMP_Text closeBtnLabel = PlayerNoteView.Label(closeBtnRect, "Label", "ЗАТВОРИ", font, 15.5f, TextAlignmentOptions.Center);
        closeBtnLabel.fontStyle = FontStyles.Bold;
        EvidenceBoardVisuals.ApplyButtonStyle(closeBtn, closeBtnImg, closeBtnLabel, EvidenceBoardVisuals.ButtonStyle.CaseMutedDanger);

        // Категории в selector-а: ДОКУМЕНТИ | СНИМКИ.
        selectorDocumentsTabButton = CreateOverlayTabButton(
            dialogInner,
            "TheorySelectorDocumentsTabButton",
            "ДОКУМЕНТИ",
            new Vector2(.022f, .785f),
            new Vector2(.24f, .845f),
            () => SelectSourceSelectorCategory(AttachedSourceType.Document));

        selectorPhotosTabButton = CreateOverlayTabButton(
            dialogInner,
            "TheorySelectorPhotosTabButton",
            "СНИМКИ",
            new Vector2(.252f, .785f),
            new Vector2(.45f, .845f),
            () => SelectSourceSelectorCategory(AttachedSourceType.Photo));
        UpdateSelectorCategoryVisuals();

        // Скролируем списък с източниците на текущото дело.
        RectTransform scrollFrame = PlayerNoteView.Rect(dialogInner, "TheorySourceSelectorScrollView",
            new Vector2(.022f, .024f), new Vector2(.978f, .772f));
        Image scrollFrameBg = scrollFrame.gameObject.AddComponent<Image>();
        scrollFrameBg.sprite = EvidenceBoardVisuals.PrintoutPaperSprite;
        scrollFrameBg.color = new Color(0.95f, 0.92f, 0.84f, 1f);
        scrollFrameBg.raycastTarget = true;

        Outline scrollOutline = scrollFrame.gameObject.AddComponent<Outline>();
        scrollOutline.effectColor = EvidenceBoardVisuals.BrassHairline;
        scrollOutline.effectDistance = new Vector2(1.5f, -1.5f);

        RectTransform viewport = PlayerNoteView.Rect(scrollFrame, "Viewport",
            new Vector2(.015f, .020f), new Vector2(.985f, .980f));
        viewport.gameObject.AddComponent<RectMask2D>();

        sourceSelectorContent = PlayerNoteView.Rect(viewport, "Content", new Vector2(0f, 1f), new Vector2(1f, 1f));
        sourceSelectorContent.pivot = new Vector2(0.5f, 1f);

        VerticalLayoutGroup contentLayout = sourceSelectorContent.gameObject.AddComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(14, 14, 12, 16);
        contentLayout.spacing = 6f;
        contentLayout.childAlignment = TextAnchor.UpperLeft;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        ContentSizeFitter contentFitter = sourceSelectorContent.gameObject.AddComponent<ContentSizeFitter>();
        contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        sourceSelectorScroll = scrollFrame.gameObject.AddComponent<ScrollRect>();
        sourceSelectorScroll.horizontal = false;
        sourceSelectorScroll.vertical = true;
        sourceSelectorScroll.viewport = viewport;
        sourceSelectorScroll.content = sourceSelectorContent;
        sourceSelectorScroll.movementType = ScrollRect.MovementType.Clamped;
        sourceSelectorScroll.inertia = true;
        sourceSelectorScroll.decelerationRate = 0.135f;
        sourceSelectorScroll.scrollSensitivity = 32f;

        var emptyGo = new GameObject("TheorySourceSelectorEmpty", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
        RectTransform emptyRect = emptyGo.GetComponent<RectTransform>();
        emptyRect.SetParent(sourceSelectorContent, false);
        emptyRect.localScale = Vector3.one;
        LayoutElement emptyLayout = emptyGo.GetComponent<LayoutElement>();
        emptyLayout.minHeight = 32f;
        emptyLayout.preferredHeight = 36f;

        TMP_Text emptyText = emptyGo.GetComponent<TMP_Text>();
        emptyText.font = font;
        emptyText.fontSize = 15.5f;
        emptyText.fontStyle = FontStyles.Italic;
        emptyText.alignment = TextAlignmentOptions.MidlineLeft;
        emptyText.color = EvidenceBoardVisuals.PaperPlaceholder;
        emptyText.raycastTarget = false;
        emptyText.text = EmptyDocumentsSelectorLabel;
        sourceSelectorEmptyText = emptyText;
        sourceSelectorEmptyLabel = emptyGo;

        sourceSelectorOverlay = overlayRect.gameObject;
        sourceSelectorOverlay.SetActive(false);
    }

    private void BuildSourcePreviewOverlay(RectTransform frame)
    {
        // Модален read-only преглед върху CaseTheoryPanel — повторно използва BoardSourcePreviewView и блокира кликовете отзад.
        RectTransform overlayRect = PlayerNoteView.Rect(frame, "TheorySourcePreviewOverlay", Vector2.zero, Vector2.one);
        Image backdrop = overlayRect.gameObject.AddComponent<Image>();
        backdrop.color = new Color(0.05f, 0.04f, 0.03f, 0.74f);
        backdrop.raycastTarget = true;

        sourcePreviewView = BoardSourcePreviewView.CreateReadOnlyOverlay(
            overlayRect,
            font,
            CloseSourcePreview,
            panelName: "TheorySourcePreviewPanel",
            documentBadge: "ПРЕГЛЕД • ОРИГИНАЛЕН ДОКУМЕНТ",
            closeLabel: "ЗАТВОРИ ПРЕГЛЕДА");

        sourcePreviewOverlay = overlayRect.gameObject;
        sourcePreviewOverlay.SetActive(false);
    }

    private BoardSourceRecord ResolveSourceForActiveCase(AttachedSourceType sourceType, string sourceId)
    {
        Func<string, BoardSourceRecord> resolver = sourceType == AttachedSourceType.Photo
            ? photoSourceResolver
            : documentSourceResolver;

        if (string.IsNullOrWhiteSpace(sourceId) || resolver == null)
            return BoardSourceRecord.CreateMissing(sourceId, sourceType);

        BoardSourceRecord resolved = resolver(sourceId);
        if (resolved == null || resolved.IsMissing || resolved.SourceType != sourceType)
            return BoardSourceRecord.CreateMissing(sourceId, sourceType);

        if (string.IsNullOrWhiteSpace(resolved.SourceLabel)
            || !string.Equals(resolved.SourceLabel, activeCaseId, StringComparison.Ordinal))
        {
            return BoardSourceRecord.CreateMissing(sourceId, sourceType);
        }

        if (sourceType == AttachedSourceType.Photo && resolved.Image == null)
            return BoardSourceRecord.CreateMissing(sourceId, sourceType);

        return resolved;
    }

    private void DeactivateActiveTheoryInputs()
    {
        if (mainSuspectInput != null && mainSuspectInput.isFocused) mainSuspectInput.DeactivateInputField();
        if (participantsInput != null && participantsInput.isFocused) participantsInput.DeactivateInputField();
        if (motiveInput != null && motiveInput.isFocused) motiveInput.DeactivateInputField();
        if (reconstructionInput != null && reconstructionInput.isFocused) reconstructionInput.DeactivateInputField();
        if (supportingEvidenceInput != null && supportingEvidenceInput.isFocused) supportingEvidenceInput.DeactivateInputField();
        if (unresolvedQuestionsInput != null && unresolvedQuestionsInput.isFocused) unresolvedQuestionsInput.DeactivateInputField();

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    private void AddReferencedSource(AttachedSourceType sourceType, string sourceId)
    {
        if (session == null || string.IsNullOrWhiteSpace(sourceId))
            return;

        SyncFromUI();
        CaseTheoryDraft draft = session.GetOrCreateDraft(activeCaseId);
        if (!draft.TryAddReferencedSource(sourceType, sourceId))
            return;

        RefreshReferencedSourcesList(draft);
        if (IsSelectorOpen)
            RebuildSourceSelectorItems();
        RebuildLayout(formContent);
    }

    private void RemoveReferencedSource(AttachedSourceType sourceType, string sourceId)
    {
        if (session == null || string.IsNullOrWhiteSpace(sourceId))
            return;

        if (!session.TryGetDraft(activeCaseId, out CaseTheoryDraft draft))
            return;

        if (!draft.RemoveReferencedSource(sourceType, sourceId))
            return;

        RefreshReferencedSourcesList(draft);
        if (IsSelectorOpen)
            RebuildSourceSelectorItems();
        RebuildLayout(formContent);
    }

    private void RefreshReferencedSourcesList(CaseTheoryDraft draft)
    {
        if (referencedSourcesList == null)
            return;

        ClearGeneratedRows(referencedSourceRows);

        bool hasDocuments = draft != null && draft.ReferencedSourceIds.Count > 0;
        bool hasPhotos = draft != null && draft.ReferencedPhotoSourceIds.Count > 0;
        bool hasReferences = hasDocuments || hasPhotos;
        if (referencedSourcesEmptyLabel != null)
            referencedSourcesEmptyLabel.SetActive(!hasReferences);

        if (!hasReferences)
            return;

        if (hasDocuments)
        {
            for (int i = 0; i < draft.ReferencedSourceIds.Count; i++)
            {
                string sourceId = draft.ReferencedSourceIds[i];
                BoardSourceRecord resolved = ResolveSourceForActiveCase(AttachedSourceType.Document, sourceId);

                bool isMissing = resolved == null || resolved.IsMissing;
                string title = isMissing ? MissingSourceLabel : resolved.Title;
                string typeMeta = isMissing
                    ? MissingSourcePreviewUnavailableLabel
                    : (string.IsNullOrWhiteSpace(resolved.SecondaryMeta)
                        ? "ДОКУМЕНТ"
                        : "ДОКУМЕНТ • " + resolved.SecondaryMeta);

                GameObject row = CreateReferencedSourceRow(AttachedSourceType.Document, sourceId, title, typeMeta, isMissing);
                referencedSourceRows.Add(row);
            }
        }

        if (hasPhotos)
        {
            for (int i = 0; i < draft.ReferencedPhotoSourceIds.Count; i++)
            {
                string photoId = draft.ReferencedPhotoSourceIds[i];
                BoardSourceRecord resolved = ResolveSourceForActiveCase(AttachedSourceType.Photo, photoId);

                bool isMissing = resolved == null || resolved.IsMissing || resolved.Image == null;
                string title = isMissing ? MissingPhotoLabel : resolved.Title;
                string typeMeta = isMissing
                    ? MissingPhotoPreviewUnavailableLabel
                    : (string.IsNullOrWhiteSpace(resolved.SecondaryMeta)
                        ? "СНИМКА"
                        : "СНИМКА • " + resolved.SecondaryMeta);

                GameObject row = CreateReferencedSourceRow(AttachedSourceType.Photo, photoId, title, typeMeta, isMissing);
                referencedSourceRows.Add(row);
            }
        }
    }

    private GameObject CreateReferencedSourceRow(AttachedSourceType sourceType, string sourceId, string title, string typeMeta, bool isMissing)
    {
        string rowPrefix = sourceType == AttachedSourceType.Photo
            ? "TheoryReferencedPhotoItem_"
            : "TheoryReferencedSourceItem_";
        var rowGo = new GameObject(rowPrefix + sourceId,
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        RectTransform rowRect = rowGo.GetComponent<RectTransform>();
        rowRect.SetParent(referencedSourcesList, false);
        rowRect.localScale = Vector3.one;

        Image rowBg = rowGo.GetComponent<Image>();
        rowBg.sprite = EvidenceBoardVisuals.ButtonSprite;
        rowBg.type = Image.Type.Sliced;
        rowBg.color = isMissing
            ? new Color(0.93f, 0.89f, 0.82f, 0.96f)
            : new Color(0.99f, 0.975f, 0.93f, 0.98f);
        rowBg.raycastTarget = false;

        Outline rowOutline = rowGo.AddComponent<Outline>();
        rowOutline.effectColor = new Color(0.45f, 0.37f, 0.25f, 0.48f);
        rowOutline.effectDistance = new Vector2(1f, -1f);

        HorizontalLayoutGroup rowLayout = rowGo.GetComponent<HorizontalLayoutGroup>();
        rowLayout.padding = new RectOffset(12, 10, 8, 8);
        rowLayout.spacing = 12f;
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;

        var infoGo = new GameObject("InfoColumn", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        RectTransform infoRect = infoGo.GetComponent<RectTransform>();
        infoRect.SetParent(rowRect, false);
        infoRect.localScale = Vector3.one;
        LayoutElement infoLayoutElem = infoGo.GetComponent<LayoutElement>();
        infoLayoutElem.flexibleWidth = 1f;

        VerticalLayoutGroup infoLayout = infoGo.GetComponent<VerticalLayoutGroup>();
        infoLayout.padding = new RectOffset(0, 0, 0, 0);
        infoLayout.spacing = 2f;
        infoLayout.childAlignment = TextAnchor.MiddleLeft;
        infoLayout.childControlWidth = true;
        infoLayout.childControlHeight = true;
        infoLayout.childForceExpandWidth = true;
        infoLayout.childForceExpandHeight = false;

        var titleGo = new GameObject("TitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
        RectTransform titleRect = titleGo.GetComponent<RectTransform>();
        titleRect.SetParent(infoRect, false);
        titleRect.localScale = Vector3.one;
        TMP_Text titleText = titleGo.GetComponent<TMP_Text>();
        titleText.font = font;
        titleText.fontSize = 16f;
        titleText.fontStyle = isMissing ? FontStyles.Italic : FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.MidlineLeft;
        titleText.color = isMissing ? EvidenceBoardVisuals.PaperPlaceholder : EvidenceBoardVisuals.PaperInk;
        titleText.textWrappingMode = TextWrappingModes.Normal;
        titleText.raycastTarget = false;
        titleText.text = title;

        if (!string.IsNullOrWhiteSpace(typeMeta))
        {
            var metaGo = new GameObject("TypeText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
            RectTransform metaRect = metaGo.GetComponent<RectTransform>();
            metaRect.SetParent(infoRect, false);
            metaRect.localScale = Vector3.one;
            TMP_Text metaText = metaGo.GetComponent<TMP_Text>();
            metaText.font = font;
            metaText.fontSize = 13.5f;
            metaText.alignment = TextAlignmentOptions.MidlineLeft;
            metaText.color = new Color(0.38f, 0.31f, 0.22f, 0.92f);
            metaText.textWrappingMode = TextWrappingModes.Normal;
            metaText.raycastTarget = false;
            metaText.text = typeMeta;
        }

        string capturedId = sourceId;
        AttachedSourceType capturedType = sourceType;
        Button previewBtn = CreateActionButton(
            rowRect,
            "PreviewTheorySourceButton",
            "ПРЕГЛЕД",
            112f,
            32f,
            14f,
            () => OpenSourcePreview(capturedType, capturedId),
            EvidenceBoardVisuals.ButtonStyle.CaseAction);
        previewBtn.navigation = new Navigation { mode = Navigation.Mode.None };
        previewBtn.interactable = !isMissing;

        Button removeBtn = CreateActionButton(
            rowRect,
            "RemoveTheorySourceButton",
            "ПРЕМАХНИ",
            124f,
            32f,
            14f,
            () => RemoveReferencedSource(capturedType, capturedId),
            EvidenceBoardVisuals.ButtonStyle.CaseMutedDanger);
        removeBtn.navigation = new Navigation { mode = Navigation.Mode.None };

        return rowGo;
    }

    private void RebuildSourceSelectorItems()
    {
        if (sourceSelectorContent == null)
            return;

        ClearGeneratedRows(selectorItemRows);

        bool isPhotoCategory = activeSelectorCategory == AttachedSourceType.Photo;
        Func<string, IReadOnlyList<BoardSourceRecord>> provider = isPhotoCategory
            ? availablePhotoSourcesProvider
            : availableSourcesProvider;

        IReadOnlyList<BoardSourceRecord> sources = provider != null
            ? provider(activeCaseId)
            : Array.Empty<BoardSourceRecord>();

        bool hasSources = sources != null && sources.Count > 0;
        if (sourceSelectorEmptyText != null)
            sourceSelectorEmptyText.text = isPhotoCategory ? EmptyPhotosSelectorLabel : EmptyDocumentsSelectorLabel;
        if (sourceSelectorEmptyLabel != null)
            sourceSelectorEmptyLabel.SetActive(!hasSources);

        if (!hasSources)
        {
            RebuildLayout(sourceSelectorContent);
            return;
        }

        CaseTheoryDraft draft = null;
        session?.TryGetDraft(activeCaseId, out draft);

        for (int i = 0; i < sources.Count; i++)
        {
            BoardSourceRecord record = sources[i];
            if (record == null || string.IsNullOrWhiteSpace(record.SourceId))
                continue;
            if (isPhotoCategory && record.Image == null)
                continue;

            bool alreadyAdded = draft != null && draft.ContainsReferencedSource(activeSelectorCategory, record.SourceId);
            GameObject row = CreateSelectorItemRow(activeSelectorCategory, record, alreadyAdded);
            selectorItemRows.Add(row);
        }

        RebuildLayout(sourceSelectorContent);
    }

    private GameObject CreateSelectorItemRow(AttachedSourceType sourceType, BoardSourceRecord record, bool alreadyAdded)
    {
        string prefix = sourceType == AttachedSourceType.Photo
            ? "TheoryPhotoSelectorItem_"
            : "TheorySourceSelectorItem_";
        var rowGo = new GameObject(prefix + record.SourceId,
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        RectTransform rowRect = rowGo.GetComponent<RectTransform>();
        rowRect.SetParent(sourceSelectorContent, false);
        rowRect.localScale = Vector3.one;

        Image rowBg = rowGo.GetComponent<Image>();
        rowBg.sprite = EvidenceBoardVisuals.ButtonSprite;
        rowBg.type = Image.Type.Sliced;
        rowBg.color = Color.white;
        rowBg.raycastTarget = true;

        Outline rowOutline = rowGo.AddComponent<Outline>();
        rowOutline.effectColor = new Color(0.45f, 0.37f, 0.25f, 0.50f);
        rowOutline.effectDistance = new Vector2(1f, -1f);

        Button button = rowGo.GetComponent<Button>();
        button.targetGraphic = rowBg;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.interactable = !alreadyAdded;

        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.99f, 0.975f, 0.93f, 0.99f);
        colors.highlightedColor = new Color(0.92f, 0.87f, 0.74f, 1f);
        colors.pressedColor = new Color(0.84f, 0.77f, 0.62f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.88f, 0.84f, 0.76f, 0.86f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        if (!alreadyAdded)
        {
            string capturedId = record.SourceId;
            AttachedSourceType capturedType = sourceType;
            button.onClick.AddListener(() => AddReferencedSource(capturedType, capturedId));
        }

        HorizontalLayoutGroup rowLayout = rowGo.GetComponent<HorizontalLayoutGroup>();
        rowLayout.padding = new RectOffset(12, 12, 8, 8);
        rowLayout.spacing = 12f;
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;

        var infoGo = new GameObject("InfoColumn", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        RectTransform infoRect = infoGo.GetComponent<RectTransform>();
        infoRect.SetParent(rowRect, false);
        infoRect.localScale = Vector3.one;
        LayoutElement infoLayoutElem = infoGo.GetComponent<LayoutElement>();
        infoLayoutElem.flexibleWidth = 1f;

        VerticalLayoutGroup infoLayout = infoGo.GetComponent<VerticalLayoutGroup>();
        infoLayout.padding = new RectOffset(0, 0, 0, 0);
        infoLayout.spacing = 2f;
        infoLayout.childAlignment = TextAnchor.MiddleLeft;
        infoLayout.childControlWidth = true;
        infoLayout.childControlHeight = true;
        infoLayout.childForceExpandWidth = true;
        infoLayout.childForceExpandHeight = false;

        var titleGo = new GameObject("TitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
        RectTransform titleRect = titleGo.GetComponent<RectTransform>();
        titleRect.SetParent(infoRect, false);
        titleRect.localScale = Vector3.one;
        TMP_Text titleText = titleGo.GetComponent<TMP_Text>();
        titleText.font = font;
        titleText.fontSize = 15.5f;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.MidlineLeft;
        titleText.color = alreadyAdded ? new Color(0.36f, 0.31f, 0.24f, 0.85f) : EvidenceBoardVisuals.PaperInk;
        titleText.textWrappingMode = TextWrappingModes.Normal;
        titleText.raycastTarget = false;
        titleText.text = record.Title;

        if (!string.IsNullOrWhiteSpace(record.SecondaryMeta))
        {
            var metaGo = new GameObject("TypeText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
            RectTransform metaRect = metaGo.GetComponent<RectTransform>();
            metaRect.SetParent(infoRect, false);
            metaRect.localScale = Vector3.one;
            TMP_Text metaText = metaGo.GetComponent<TMP_Text>();
            metaText.font = font;
            metaText.fontSize = 13.5f;
            metaText.alignment = TextAlignmentOptions.MidlineLeft;
            metaText.color = new Color(0.40f, 0.34f, 0.25f, 0.88f);
            metaText.textWrappingMode = TextWrappingModes.Normal;
            metaText.raycastTarget = false;
            metaText.text = record.SecondaryMeta;
        }

        var statusGo = new GameObject("StatusText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
        RectTransform statusRect = statusGo.GetComponent<RectTransform>();
        statusRect.SetParent(rowRect, false);
        statusRect.localScale = Vector3.one;
        LayoutElement statusLayout = statusGo.GetComponent<LayoutElement>();
        statusLayout.minWidth = 104f;
        statusLayout.preferredWidth = 104f;
        statusLayout.minHeight = 26f;

        TMP_Text statusText = statusGo.GetComponent<TMP_Text>();
        statusText.font = font;
        statusText.fontSize = 14f;
        statusText.fontStyle = FontStyles.Bold;
        statusText.alignment = TextAlignmentOptions.MidlineRight;
        statusText.color = alreadyAdded
            ? new Color(0.38f, 0.33f, 0.26f, 0.85f)
            : new Color(0.16f, 0.26f, 0.24f, 1f);
        statusText.textWrappingMode = TextWrappingModes.NoWrap;
        statusText.raycastTarget = false;
        statusText.text = alreadyAdded ? "ДОБАВЕН" : "ДОБАВИ";

        return rowGo;
    }

    private Button CreateActionButton(
        Transform parent,
        string name,
        string labelText,
        float width,
        float height,
        float fontSize,
        UnityEngine.Events.UnityAction onClick,
        EvidenceBoardVisuals.ButtonStyle style)
    {
        var btnGo = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
        RectTransform btnRect = btnGo.GetComponent<RectTransform>();
        btnRect.SetParent(parent, false);
        btnRect.localScale = Vector3.one;

        LayoutElement layout = btnGo.GetComponent<LayoutElement>();
        layout.minWidth = width;
        layout.preferredWidth = width;
        layout.minHeight = height;
        layout.preferredHeight = height;

        Image bg = btnGo.GetComponent<Image>();
        Button btn = btnGo.GetComponent<Button>();
        btn.targetGraphic = bg;
        btn.onClick.AddListener(onClick);

        TMP_Text label = PlayerNoteView.Label(btnRect, "Label", labelText, font, fontSize, TextAlignmentOptions.Center);
        label.fontStyle = FontStyles.Bold;
        EvidenceBoardVisuals.ApplyButtonStyle(btn, bg, label, style);
        return btn;
    }

    private Button CreateOverlayTabButton(
        Transform parent,
        string name,
        string labelText,
        Vector2 anchorMin,
        Vector2 anchorMax,
        UnityEngine.Events.UnityAction onClick)
    {
        RectTransform rect = PlayerNoteView.Rect(parent, name, anchorMin, anchorMax);
        Image bg = rect.gameObject.AddComponent<Image>();
        Button btn = rect.gameObject.AddComponent<Button>();
        btn.targetGraphic = bg;
        btn.navigation = new Navigation { mode = Navigation.Mode.None };
        btn.onClick.AddListener(onClick);
        TMP_Text label = PlayerNoteView.Label(rect, "Label", labelText, font, 14.5f, TextAlignmentOptions.Center);
        label.fontStyle = FontStyles.Bold;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
        EvidenceBoardVisuals.ApplyButtonStyle(btn, bg, label, EvidenceBoardVisuals.ButtonStyle.CorkControl);
        return btn;
    }

    private void UpdateSelectorCategoryVisuals()
    {
        if (selectorDocumentsTabButton != null)
        {
            EvidenceBoardVisuals.ApplyButtonStyle(
                selectorDocumentsTabButton,
                (Image)selectorDocumentsTabButton.targetGraphic,
                selectorDocumentsTabButton.GetComponentInChildren<TMP_Text>(),
                activeSelectorCategory == AttachedSourceType.Document
                    ? EvidenceBoardVisuals.ButtonStyle.PrimaryManila
                    : EvidenceBoardVisuals.ButtonStyle.CorkControl);
        }

        if (selectorPhotosTabButton != null)
        {
            EvidenceBoardVisuals.ApplyButtonStyle(
                selectorPhotosTabButton,
                (Image)selectorPhotosTabButton.targetGraphic,
                selectorPhotosTabButton.GetComponentInChildren<TMP_Text>(),
                activeSelectorCategory == AttachedSourceType.Photo
                    ? EvidenceBoardVisuals.ButtonStyle.PrimaryManila
                    : EvidenceBoardVisuals.ButtonStyle.CorkControl);
        }
    }

    private static void ClearGeneratedRows(List<GameObject> rows)
    {
        for (int i = 0; i < rows.Count; i++)
        {
            GameObject row = rows[i];
            if (row == null) continue;
            row.SetActive(false);
            row.transform.SetParent(null, false);
            Destroy(row);
        }

        rows.Clear();
    }

    private static void RebuildLayout(RectTransform target)
    {
        if (target == null)
            return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(target);
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(target);
    }

    private TMP_InputField CreateField(
        string fieldPrefix,
        string labelText,
        string placeholderText,
        float inputHeight,
        bool multiline,
        int charLimit,
        UnityEngine.Events.UnityAction<string> onChanged)
    {
        var groupGo = new GameObject(fieldPrefix + "Group", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        RectTransform groupRect = groupGo.GetComponent<RectTransform>();
        groupRect.SetParent(formContent, false);
        groupRect.localScale = Vector3.one;

        VerticalLayoutGroup groupLayout = groupGo.GetComponent<VerticalLayoutGroup>();
        groupLayout.padding = new RectOffset(0, 0, 0, 0);
        groupLayout.spacing = 4f;
        groupLayout.childAlignment = TextAnchor.UpperLeft;
        groupLayout.childControlWidth = true;
        groupLayout.childControlHeight = true;
        groupLayout.childForceExpandWidth = true;
        groupLayout.childForceExpandHeight = false;

        var labelGo = new GameObject(fieldPrefix + "Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
        RectTransform labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.SetParent(groupRect, false);
        labelRect.localScale = Vector3.one;
        LayoutElement labelLayout = labelGo.GetComponent<LayoutElement>();
        labelLayout.minHeight = 24f;
        labelLayout.preferredHeight = 26f;

        TMP_Text label = labelGo.GetComponent<TMP_Text>();
        label.font = font;
        label.fontSize = 15.5f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.color = new Color(0.24f, 0.18f, 0.12f, 1f);
        label.raycastTarget = false;
        label.text = labelText;

        var inputGo = new GameObject(fieldPrefix + "Input", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement), typeof(TMP_InputField));
        RectTransform inputRect = inputGo.GetComponent<RectTransform>();
        inputRect.SetParent(groupRect, false);
        inputRect.localScale = Vector3.one;

        LayoutElement inputLayout = inputGo.GetComponent<LayoutElement>();
        inputLayout.minHeight = inputHeight;
        inputLayout.preferredHeight = inputHeight;
        inputLayout.flexibleWidth = 1f;

        Image bg = inputGo.GetComponent<Image>();
        bg.sprite = EvidenceBoardVisuals.ButtonSprite;
        bg.type = Image.Type.Sliced;
        bg.color = new Color(0.99f, 0.975f, 0.93f, 0.98f);
        bg.raycastTarget = true;

        Outline outline = inputGo.AddComponent<Outline>();
        outline.effectColor = new Color(0.45f, 0.37f, 0.25f, 0.55f);
        outline.effectDistance = new Vector2(1f, -1f);

        RectTransform textArea = PlayerNoteView.Rect(inputRect, "TextArea", Vector2.zero, Vector2.one);
        textArea.offsetMin = new Vector2(10f, 8f);
        textArea.offsetMax = new Vector2(-10f, -8f);
        textArea.gameObject.AddComponent<RectMask2D>();

        TMP_Text textComponent = PlayerNoteView.Label(textArea, "Text", string.Empty, font, 17f,
            multiline ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.MidlineLeft);
        textComponent.color = EvidenceBoardVisuals.PaperInk;
        textComponent.richText = false;
        textComponent.lineSpacing = 4f;
        textComponent.textWrappingMode = multiline ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;

        TMP_Text placeholder = PlayerNoteView.Label(textArea, "Placeholder", placeholderText, font, 16f,
            multiline ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.MidlineLeft);
        placeholder.color = EvidenceBoardVisuals.PaperPlaceholder;
        placeholder.fontStyle = FontStyles.Italic;
        placeholder.textWrappingMode = multiline ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;

        TMP_InputField input = inputGo.GetComponent<TMP_InputField>();
        input.targetGraphic = bg;
        input.textViewport = textArea;
        input.textComponent = textComponent;
        input.placeholder = placeholder;
        input.richText = false;
        input.caretColor = EvidenceBoardVisuals.PaperInk;
        input.selectionColor = EvidenceBoardVisuals.PaperSelection;
        input.lineType = multiline ? TMP_InputField.LineType.MultiLineNewline : TMP_InputField.LineType.SingleLine;
        input.characterLimit = charLimit;
        input.navigation = new Navigation { mode = Navigation.Mode.None };
        input.onValueChanged.AddListener(onChanged);

        return input;
    }
}
