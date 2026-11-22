using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Управлява екрана за ръчна времева линия (EB-006): хронологичен списък по дати,
// отделна група "Без уточнен час", форма за създаване/редакция и потвърждение за изтриване.
public sealed class BoardTimelineView : MonoBehaviour
{
    private readonly List<GameObject> feedObjects = new List<GameObject>();
    private readonly List<TimelineEventView> eventViews = new List<TimelineEventView>();

    private TimelineSession session;
    private TMP_FontAsset font;

    private ScrollRect feedScroll;
    private RectTransform feedContent;
    private GameObject emptyState;

    private TMP_Text editorHeaderText;
    private TMP_InputField dateInput;
    private TMP_InputField timeInput;
    private TMP_InputField titleInput;
    private TMP_InputField descriptionInput;
    private TMP_Text validationText;
    private TMP_Text saveButtonLabel;
    private TMP_Text cancelButtonLabel;

    private GameObject deleteConfirmDialog;
    private string pendingDeleteId;
    private string editingEventId;

    public bool IsOpen => gameObject.activeSelf;

    public static BoardTimelineView Create(Transform boardRoot, TimelineSession session, TMP_FontAsset font)
    {
        RectTransform frame = PlayerNoteView.Rect(boardRoot, "InvestigationTimelinePanel",
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

        BoardTimelineView view = frame.gameObject.AddComponent<BoardTimelineView>();
        view.session = session;
        view.font = font;
        view.Build(frame);
        frame.gameObject.SetActive(false);
        return view;
    }

    public void Show()
    {
        CancelDelete();
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        RebuildFromSession();
    }

    public void Hide()
    {
        CancelDelete();
        if (gameObject.activeSelf)
            gameObject.SetActive(false);
    }

    public void RebuildFromSession()
    {
        CancelDelete();
        ClearFeed();

        bool hasEvents = session != null && session.Events.Count > 0;
        if (emptyState != null)
            emptyState.SetActive(!hasEvents);

        if (hasEvents)
        {
            string currentDate = null;
            bool untimedHeaderAddedForDate = false;

            for (int i = 0; i < session.Events.Count; i++)
            {
                TimelineEvent item = session.Events[i];
                if (!string.Equals(currentDate, item.Date, System.StringComparison.Ordinal))
                {
                    currentDate = item.Date;
                    untimedHeaderAddedForDate = false;
                    CreateDateHeader(currentDate);
                }

                if (!item.HasTime && !untimedHeaderAddedForDate)
                {
                    untimedHeaderAddedForDate = true;
                    CreateUntimedGroupHeader(currentDate);
                }

                TimelineEventView card = TimelineEventView.Create(
                    feedContent, item, font, BeginEditEvent, RequestDeleteEvent);
                card.SetSelected(!string.IsNullOrEmpty(editingEventId)
                    && string.Equals(item.Id, editingEventId, System.StringComparison.Ordinal));
                eventViews.Add(card);
                feedObjects.Add(card.gameObject);
            }
        }

        if (!string.IsNullOrEmpty(editingEventId) && (session == null || !session.TryGetById(editingEventId, out _)))
        {
            ResetEditorToCreateMode(clearFields: true);
        }

        if (feedContent != null)
        {
            // Двупроходно преизчисляване, за да се установят ширините и след това височините при многоредови заглавия/описания.
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(feedContent);
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(feedContent);
        }
    }

    private void ClearFeed()
    {
        for (int i = 0; i < feedObjects.Count; i++)
        {
            GameObject obj = feedObjects[i];
            if (obj == null) continue;
            obj.SetActive(false);
            obj.transform.SetParent(null, false);
            Destroy(obj);
        }
        feedObjects.Clear();
        eventViews.Clear();
    }

    private void Build(RectTransform frame)
    {
        RectTransform inner = PlayerNoteView.Rect(frame, "InnerSurface", new Vector2(.010f, .014f), new Vector2(.990f, .986f));
        Image innerImg = inner.gameObject.AddComponent<Image>();
        innerImg.color = new Color(0.115f, 0.095f, 0.080f, 0.98f);
        innerImg.raycastTarget = true;

        // Горна лайсна с "+ Ново събитие" и заглавие.
        RectTransform headerRail = PlayerNoteView.Rect(inner, "TimelineHeaderRail", new Vector2(0f, .908f), new Vector2(1f, 1f));
        Image headerRailImg = headerRail.gameObject.AddComponent<Image>();
        headerRailImg.color = EvidenceBoardVisuals.HeaderRailColor;
        headerRailImg.raycastTarget = false;

        RectTransform headerAccent = PlayerNoteView.Rect(inner, "TimelineHeaderAccent", new Vector2(0f, .905f), new Vector2(1f, .909f));
        Image headerAccentImg = headerAccent.gameObject.AddComponent<Image>();
        headerAccentImg.color = EvidenceBoardVisuals.BrassHairline;
        headerAccentImg.raycastTarget = false;

        CreateButton(inner, "NewTimelineEventButton", "+ Ново събитие",
            new Vector2(.018f, .918f), new Vector2(.225f, .988f),
            HandleNewEventClicked, EvidenceBoardVisuals.ButtonStyle.PrimaryManila, 18f);

        RectTransform headerLabelRect = PlayerNoteView.Rect(inner, "TimelineHeaderLabel",
            new Vector2(.25f, .918f), new Vector2(.982f, .988f));
        TMP_Text headerLabel = PlayerNoteView.Label(headerLabelRect, "Text",
            "ВРЕМЕВА ЛИНИЯ НА РАЗСЛЕДВАНЕТО — хронология по въведени дата и час",
            font, 15.5f, TextAlignmentOptions.MidlineRight);
        headerLabel.color = EvidenceBoardVisuals.MutedLabelText;
        headerLabel.textWrappingMode = TextWrappingModes.NoWrap;
        headerLabel.overflowMode = TextOverflowModes.Ellipsis;

        BuildFeedColumn(inner);
        BuildEditorColumn(inner);
        BuildDeleteConfirmDialog(inner);
        ResetEditorToCreateMode(clearFields: true);
    }

    private void BuildFeedColumn(RectTransform inner)
    {
        RectTransform feedFrame = PlayerNoteView.Rect(inner, "TimelineListPanel",
            new Vector2(.018f, .022f), new Vector2(.595f, .888f));
        Image feedBg = feedFrame.gameObject.AddComponent<Image>();
        feedBg.sprite = EvidenceBoardVisuals.CorkSprite;
        feedBg.type = Image.Type.Tiled;
        feedBg.pixelsPerUnitMultiplier = 1.25f;
        feedBg.color = new Color(0.70f, 0.65f, 0.60f, 1f);
        feedBg.raycastTarget = true;

        Outline feedOutline = feedFrame.gameObject.AddComponent<Outline>();
        feedOutline.effectColor = EvidenceBoardVisuals.BrassHairline;
        feedOutline.effectDistance = new Vector2(1f, -1f);

        RectTransform viewport = PlayerNoteView.Rect(feedFrame, "Viewport",
            new Vector2(.018f, .020f), new Vector2(.982f, .980f));
        viewport.gameObject.AddComponent<RectMask2D>();

        feedContent = PlayerNoteView.Rect(viewport, "Content", new Vector2(0f, 1f), new Vector2(1f, 1f));
        feedContent.pivot = new Vector2(0.5f, 1f);

        VerticalLayoutGroup vLayout = feedContent.gameObject.AddComponent<VerticalLayoutGroup>();
        vLayout.padding = new RectOffset(6, 6, 6, 12);
        vLayout.spacing = 8f;
        vLayout.childAlignment = TextAnchor.UpperLeft;
        vLayout.childControlWidth = true;
        vLayout.childControlHeight = true;
        vLayout.childForceExpandWidth = true;
        vLayout.childForceExpandHeight = false;

        ContentSizeFitter fitter = feedContent.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        feedScroll = feedFrame.gameObject.AddComponent<ScrollRect>();
        feedScroll.horizontal = false;
        feedScroll.vertical = true;
        feedScroll.viewport = viewport;
        feedScroll.content = feedContent;
        feedScroll.movementType = ScrollRect.MovementType.Clamped;
        feedScroll.scrollSensitivity = 28f;

        // Празна подсказка, когато няма въведени събития.
        RectTransform emptyCard = PlayerNoteView.Rect(feedFrame, "TimelineEmptyHint",
            new Vector2(.14f, .36f), new Vector2(.86f, .64f));
        Image emptyBg = emptyCard.gameObject.AddComponent<Image>();
        emptyBg.sprite = EvidenceBoardVisuals.ButtonSprite;
        emptyBg.type = Image.Type.Sliced;
        emptyBg.color = new Color(0.11f, 0.085f, 0.06f, 0.78f);
        emptyBg.raycastTarget = false;

        RectTransform emptyTextBox = PlayerNoteView.Rect(emptyCard, "TextBox",
            new Vector2(.06f, .10f), new Vector2(.94f, .90f));
        TMP_Text emptyHint = PlayerNoteView.Label(emptyTextBox, "Text",
            "Няма добавени събития във времевата линия.\nНатиснете „+ Ново събитие“ или попълнете формата вдясно.",
            font, 17.5f, TextAlignmentOptions.Center);
        emptyHint.color = EvidenceBoardVisuals.ParchmentText;
        emptyHint.lineSpacing = 6f;
        emptyHint.textWrappingMode = TextWrappingModes.Normal;
        emptyState = emptyCard.gameObject;
    }

    private void BuildEditorColumn(RectTransform inner)
    {
        RectTransform editor = PlayerNoteView.Rect(inner, "TimelineEditorPanel",
            new Vector2(.612f, .022f), new Vector2(.982f, .888f));
        Image editorBg = editor.gameObject.AddComponent<Image>();
        editorBg.sprite = EvidenceBoardVisuals.ButtonSprite;
        editorBg.type = Image.Type.Sliced;
        editorBg.color = new Color(0.145f, 0.120f, 0.100f, 0.96f);
        editorBg.raycastTarget = true;

        Outline editorOutline = editor.gameObject.AddComponent<Outline>();
        editorOutline.effectColor = EvidenceBoardVisuals.BrassHairline;
        editorOutline.effectDistance = new Vector2(1.2f, -1.2f);

        RectTransform headerRect = PlayerNoteView.Rect(editor, "TimelineEditorHeader",
            new Vector2(.05f, .91f), new Vector2(.95f, .975f));
        editorHeaderText = PlayerNoteView.Label(headerRect, "Text", "НОВО СЪБИТИЕ",
            font, 18.5f, TextAlignmentOptions.MidlineLeft);
        editorHeaderText.color = EvidenceBoardVisuals.StatusTextColor;
        editorHeaderText.fontStyle = FontStyles.Bold;

        // Поле 1: Дата (YYYY-MM-DD).
        CreateFieldLabel(editor, "DateFieldLabel", "ДАТА (YYYY-MM-DD) *", new Vector2(.05f, .855f), new Vector2(.51f, .900f));
        dateInput = CreateInputField(editor, "TimelineDateInput", "2003-10-14",
            new Vector2(.05f, .785f), new Vector2(.51f, .852f), multiline: false, charLimit: 16);

        // Поле 2: Час (HH:mm, по избор).
        CreateFieldLabel(editor, "TimeFieldLabel", "ЧАС (HH:mm, по избор)", new Vector2(.54f, .855f), new Vector2(.95f, .900f));
        timeInput = CreateInputField(editor, "TimelineTimeInput", "23:45 или празно",
            new Vector2(.54f, .785f), new Vector2(.95f, .852f), multiline: false, charLimit: 10);

        // Поле 3: Заглавие.
        CreateFieldLabel(editor, "TitleFieldLabel", "ЗАГЛАВИЕ *", new Vector2(.05f, .722f), new Vector2(.95f, .767f));
        titleInput = CreateInputField(editor, "TimelineTitleInput", "Кратко заглавие на събитието",
            new Vector2(.05f, .648f), new Vector2(.95f, .719f), multiline: false, charLimit: 120);

        // Поле 4: Описание.
        CreateFieldLabel(editor, "DescriptionFieldLabel", "ОПИСАНИЕ (свободен текст)", new Vector2(.05f, .585f), new Vector2(.95f, .630f));
        descriptionInput = CreateInputField(editor, "TimelineDescriptionInput",
            "Опишете какво смятате, че се е случило...",
            new Vector2(.05f, .240f), new Vector2(.95f, .582f), multiline: true, charLimit: 1000);

        // Статус / валидация (само формат и задължителни полета).
        RectTransform valRect = PlayerNoteView.Rect(editor, "TimelineValidationText",
            new Vector2(.05f, .135f), new Vector2(.95f, .228f));
        validationText = PlayerNoteView.Label(valRect, "Text", "", font, 14.5f, TextAlignmentOptions.MidlineLeft);
        validationText.color = EvidenceBoardVisuals.StatusTextColor;
        validationText.textWrappingMode = TextWrappingModes.Normal;

        // Бутони Запази / Изчисти.
        Button saveBtn = CreateButton(editor, "SaveTimelineEventButton", "Запази",
            new Vector2(.05f, .035f), new Vector2(.52f, .118f),
            HandleSaveClicked, EvidenceBoardVisuals.ButtonStyle.PrimaryManila, 18f);
        saveButtonLabel = saveBtn.GetComponentInChildren<TMP_Text>();

        Button cancelBtn = CreateButton(editor, "CancelTimelineEditButton", "Изчисти",
            new Vector2(.56f, .035f), new Vector2(.95f, .118f),
            HandleCancelEditClicked, EvidenceBoardVisuals.ButtonStyle.CorkControl, 17f);
        cancelButtonLabel = cancelBtn.GetComponentInChildren<TMP_Text>();
    }

    private void BuildDeleteConfirmDialog(RectTransform inner)
    {
        RectTransform overlay = PlayerNoteView.Rect(inner, "DeleteTimelineEventConfirmDialog",
            Vector2.zero, Vector2.one);
        Image backdrop = overlay.gameObject.AddComponent<Image>();
        backdrop.color = new Color(0.06f, 0.045f, 0.035f, 0.74f);
        backdrop.raycastTarget = true;

        RectTransform card = PlayerNoteView.Rect(overlay, "DialogCard",
            new Vector2(.28f, .36f), new Vector2(.72f, .64f));
        Image cardBg = card.gameObject.AddComponent<Image>();
        cardBg.sprite = EvidenceBoardVisuals.ButtonSprite;
        cardBg.type = Image.Type.Sliced;
        cardBg.color = new Color(0.15f, 0.12f, 0.10f, 0.98f);
        cardBg.raycastTarget = true;

        Outline cardOutline = card.gameObject.AddComponent<Outline>();
        cardOutline.effectColor = EvidenceBoardVisuals.BrassAccent;
        cardOutline.effectDistance = new Vector2(1.5f, -1.5f);

        Shadow cardShadow = card.gameObject.AddComponent<Shadow>();
        cardShadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
        cardShadow.effectDistance = new Vector2(0f, -6f);

        RectTransform promptBox = PlayerNoteView.Rect(card, "DeleteConfirmPrompt",
            new Vector2(.08f, .48f), new Vector2(.92f, .86f));
        TMP_Text prompt = PlayerNoteView.Label(promptBox, "Text",
            "Да изтрия ли това събитие от времевата линия?",
            font, 19f, TextAlignmentOptions.Center);
        prompt.color = EvidenceBoardVisuals.ParchmentText;
        prompt.textWrappingMode = TextWrappingModes.Normal;

        CreateButton(card, "ConfirmDeleteTimelineEventButton", "Изтрий",
            new Vector2(.10f, .14f), new Vector2(.46f, .40f),
            ConfirmDelete, EvidenceBoardVisuals.ButtonStyle.CaseMutedDanger, 18f);
        CreateButton(card, "CancelDeleteTimelineEventButton", "Отказ",
            new Vector2(.54f, .14f), new Vector2(.90f, .40f),
            CancelDelete, EvidenceBoardVisuals.ButtonStyle.CorkControl, 18f);

        deleteConfirmDialog = overlay.gameObject;
        deleteConfirmDialog.SetActive(false);
    }

    private void CreateDateHeader(string dateText)
    {
        var headerObj = new GameObject("TimelineDateHeader-" + dateText, typeof(RectTransform));
        headerObj.transform.SetParent(feedContent, false);

        LayoutElement layout = headerObj.AddComponent<LayoutElement>();
        layout.minHeight = 34f;
        layout.preferredHeight = 34f;

        Image bg = headerObj.AddComponent<Image>();
        bg.color = new Color(0.14f, 0.11f, 0.085f, 0.92f);
        bg.raycastTarget = false;

        RectTransform underline = PlayerNoteView.Rect(headerObj.transform, "Underline",
            new Vector2(0f, 0f), new Vector2(1f, .06f));
        Image underlineImg = underline.gameObject.AddComponent<Image>();
        underlineImg.color = EvidenceBoardVisuals.BrassAccent;
        underlineImg.raycastTarget = false;

        RectTransform labelBox = PlayerNoteView.Rect(headerObj.transform, "LabelBox",
            new Vector2(.025f, .14f), new Vector2(.975f, .94f));
        TMP_Text label = PlayerNoteView.Label(labelBox, "Text", dateText,
            font, 18f, TextAlignmentOptions.MidlineLeft);
        label.color = EvidenceBoardVisuals.StatusTextColor;
        label.fontStyle = FontStyles.Bold;
        label.textWrappingMode = TextWrappingModes.NoWrap;

        feedObjects.Add(headerObj);
    }

    private void CreateUntimedGroupHeader(string dateText)
    {
        var groupObj = new GameObject("TimelineUntimedGroupHeader-" + dateText, typeof(RectTransform));
        groupObj.transform.SetParent(feedContent, false);

        LayoutElement layout = groupObj.AddComponent<LayoutElement>();
        layout.minHeight = 28f;
        layout.preferredHeight = 28f;

        Image bg = groupObj.AddComponent<Image>();
        bg.color = new Color(0.18f, 0.145f, 0.115f, 0.85f);
        bg.raycastTarget = false;

        RectTransform labelBox = PlayerNoteView.Rect(groupObj.transform, "LabelBox",
            new Vector2(.045f, .05f), new Vector2(.975f, .95f));
        TMP_Text label = PlayerNoteView.Label(labelBox, "Text", "Без уточнен час",
            font, 14.5f, TextAlignmentOptions.MidlineLeft);
        label.color = EvidenceBoardVisuals.MutedLabelText;
        label.fontStyle = FontStyles.Italic;
        label.textWrappingMode = TextWrappingModes.NoWrap;

        feedObjects.Add(groupObj);
    }

    private void HandleNewEventClicked()
    {
        CancelDelete();
        ResetEditorToCreateMode(clearFields: true);
        dateInput?.ActivateInputField();
    }

    private void BeginEditEvent(TimelineEventView target)
    {
        if (target == null || target.Model == null) return;
        CancelDelete();
        editingEventId = target.EventId;
        editorHeaderText.text = "РЕДАКТИРАНЕ НА СЪБИТИЕ";
        saveButtonLabel.text = "Обнови";
        cancelButtonLabel.text = "Отказ";
        dateInput.SetTextWithoutNotify(target.Model.Date);
        timeInput.SetTextWithoutNotify(target.Model.Time);
        titleInput.SetTextWithoutNotify(target.Model.Title);
        descriptionInput.SetTextWithoutNotify(target.Model.Description);
        validationText.text = string.Empty;
        UpdateSelectionHighlights();
    }

    private void HandleSaveClicked()
    {
        if (session == null) return;
        CancelDelete();

        string date = dateInput.text;
        string time = timeInput.text;
        string title = titleInput.text;
        string description = descriptionInput.text;

        if (string.IsNullOrEmpty(editingEventId))
        {
            if (!session.TryCreate(date, time, title, description, out _, out string error))
            {
                validationText.text = error;
                return;
            }
        }
        else
        {
            if (!session.TryUpdate(editingEventId, date, time, title, description, out string error))
            {
                validationText.text = error;
                return;
            }
        }

        ResetEditorToCreateMode(clearFields: true);
        RebuildFromSession();
    }

    private void HandleCancelEditClicked()
    {
        CancelDelete();
        ResetEditorToCreateMode(clearFields: true);
        UpdateSelectionHighlights();
    }

    private void RequestDeleteEvent(TimelineEventView target)
    {
        if (target == null) return;
        pendingDeleteId = target.EventId;
        if (deleteConfirmDialog != null)
        {
            deleteConfirmDialog.transform.SetAsLastSibling();
            deleteConfirmDialog.SetActive(true);
        }
    }

    private void ConfirmDelete()
    {
        string targetId = pendingDeleteId;
        CancelDelete();
        if (string.IsNullOrEmpty(targetId) || session == null) return;

        session.Remove(targetId);
        if (string.Equals(editingEventId, targetId, System.StringComparison.Ordinal))
        {
            ResetEditorToCreateMode(clearFields: true);
        }
        RebuildFromSession();
    }

    private void CancelDelete()
    {
        pendingDeleteId = null;
        if (deleteConfirmDialog != null)
            deleteConfirmDialog.SetActive(false);
    }

    private void ResetEditorToCreateMode(bool clearFields)
    {
        editingEventId = null;
        if (editorHeaderText != null) editorHeaderText.text = "НОВО СЪБИТИЕ";
        if (saveButtonLabel != null) saveButtonLabel.text = "Запази";
        if (cancelButtonLabel != null) cancelButtonLabel.text = "Изчисти";
        if (validationText != null) validationText.text = string.Empty;
        if (clearFields)
        {
            dateInput?.SetTextWithoutNotify(string.Empty);
            timeInput?.SetTextWithoutNotify(string.Empty);
            titleInput?.SetTextWithoutNotify(string.Empty);
            descriptionInput?.SetTextWithoutNotify(string.Empty);
        }
    }

    private void UpdateSelectionHighlights()
    {
        for (int i = 0; i < eventViews.Count; i++)
        {
            TimelineEventView card = eventViews[i];
            if (card == null) continue;
            card.SetSelected(!string.IsNullOrEmpty(editingEventId)
                && string.Equals(card.EventId, editingEventId, System.StringComparison.Ordinal));
        }
    }

    private void OnDisable() => CancelDelete();

    private void CreateFieldLabel(RectTransform parent, string name, string text, Vector2 min, Vector2 max)
    {
        RectTransform rect = PlayerNoteView.Rect(parent, name, min, max);
        TMP_Text label = PlayerNoteView.Label(rect, "Text", text, font, 14f, TextAlignmentOptions.MidlineLeft);
        label.color = EvidenceBoardVisuals.MutedLabelText;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
    }

    private TMP_InputField CreateInputField(RectTransform parent, string name, string placeholderValue,
        Vector2 min, Vector2 max, bool multiline, int charLimit)
    {
        RectTransform rect = PlayerNoteView.Rect(parent, name, min, max);
        Image bg = rect.gameObject.AddComponent<Image>();
        bg.sprite = EvidenceBoardVisuals.ButtonSprite;
        bg.type = Image.Type.Sliced;
        bg.color = new Color(0.95f, 0.92f, 0.84f, 0.98f);
        bg.raycastTarget = true;

        RectTransform viewport = PlayerNoteView.Rect(rect, "TextViewport",
            multiline ? new Vector2(.03f, .05f) : new Vector2(.03f, .10f),
            multiline ? new Vector2(.97f, .95f) : new Vector2(.97f, .90f));
        viewport.gameObject.AddComponent<RectMask2D>();

        TMP_Text text = PlayerNoteView.Label(viewport, "Text", "", font, 16.5f,
            multiline ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.MidlineLeft);
        text.color = EvidenceBoardVisuals.PaperInk;
        text.lineSpacing = multiline ? 4f : 0f;
        text.textWrappingMode = multiline ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;

        TMP_Text placeholder = PlayerNoteView.Label(viewport, "Placeholder", placeholderValue, font, 16f,
            multiline ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.MidlineLeft);
        placeholder.color = EvidenceBoardVisuals.PaperPlaceholder;
        placeholder.fontStyle = FontStyles.Italic;
        placeholder.textWrappingMode = multiline ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;

        TMP_InputField input = rect.gameObject.AddComponent<TMP_InputField>();
        input.targetGraphic = bg;
        input.textViewport = viewport;
        input.textComponent = text;
        input.placeholder = placeholder;
        input.lineType = multiline ? TMP_InputField.LineType.MultiLineNewline : TMP_InputField.LineType.SingleLine;
        input.characterLimit = charLimit;
        input.richText = false;
        input.restoreOriginalTextOnEscape = false;
        input.onFocusSelectAll = false;
        input.customCaretColor = true;
        input.caretColor = EvidenceBoardVisuals.PaperInk;
        input.selectionColor = EvidenceBoardVisuals.PaperSelection;
        input.navigation = new Navigation { mode = Navigation.Mode.None };
        return input;
    }

    private Button CreateButton(Transform parent, string name, string title, Vector2 min, Vector2 max,
        UnityEngine.Events.UnityAction click, EvidenceBoardVisuals.ButtonStyle style, float fontSize)
    {
        RectTransform rect = PlayerNoteView.Rect(parent, name, min, max);
        Image image = rect.gameObject.AddComponent<Image>();
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.onClick.AddListener(click);
        TMP_Text label = PlayerNoteView.Label(rect, "Label", title, font, fontSize, TextAlignmentOptions.Center);
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
        EvidenceBoardVisuals.ApplyButtonStyle(button, image, label, style);
        return button;
    }
}
