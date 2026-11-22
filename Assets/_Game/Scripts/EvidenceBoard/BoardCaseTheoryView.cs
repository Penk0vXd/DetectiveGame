using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Управлява екрана „МОЯТА ТЕОРИЯ — РАБОТНА ЧЕРНОВА“ в Evidence Board (EB-013).
// Представя 6 свободно редактируеми текстови полета без проверка за правилност и без официално подаване.
public sealed class BoardCaseTheoryView : MonoBehaviour
{
    private const int ShortFieldLimit = 300;
    private const int MediumFieldLimit = 1500;
    private const int LongFieldLimit = 3000;

    private CaseTheorySession session;
    private string activeCaseId = CaseTheorySession.DefaultCaseId;
    private TMP_FontAsset font;

    private ScrollRect formScroll;
    private RectTransform formContent;

    private TMP_InputField mainSuspectInput;
    private TMP_InputField participantsInput;
    private TMP_InputField motiveInput;
    private TMP_InputField reconstructionInput;
    private TMP_InputField supportingEvidenceInput;
    private TMP_InputField unresolvedQuestionsInput;

    public bool IsOpen => gameObject.activeSelf;
    public string ActiveCaseId => activeCaseId;

    public static BoardCaseTheoryView Create(
        Transform boardRoot,
        CaseTheorySession session,
        TMP_FontAsset font,
        string caseId = CaseTheorySession.DefaultCaseId)
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
        view.activeCaseId = string.IsNullOrWhiteSpace(caseId) ? CaseTheorySession.DefaultCaseId : caseId.Trim();
        view.Build(frame);
        frame.gameObject.SetActive(false);
        return view;
    }

    public void Show()
    {
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        RefreshFromSession();
    }

    public void Hide()
    {
        SyncFromUI();
        if (gameObject.activeSelf)
            gameObject.SetActive(false);
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

        if (formContent != null)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(formContent);
        }
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
