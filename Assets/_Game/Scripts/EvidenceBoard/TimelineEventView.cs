using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Визуализира едно ръчно събитие във времевата линия като хартиена карта (2003 стил).
public sealed class TimelineEventView : MonoBehaviour
{
    private TimelineEvent model;
    private Outline selectionOutline;
    private Button editButton;
    private Button deleteButton;
    private Action<TimelineEventView> editRequested;
    private Action<TimelineEventView> deleteRequested;

    public string EventId => model.Id;
    public TimelineEvent Model => model;

    public static TimelineEventView Create(
        Transform parent,
        TimelineEvent model,
        TMP_FontAsset font,
        Action<TimelineEventView> onEditRequested,
        Action<TimelineEventView> onDeleteRequested)
    {
        var rowObj = new GameObject("TimelineEvent-" + model.Id, typeof(RectTransform));
        rowObj.transform.SetParent(parent, false);

        HorizontalLayoutGroup rowLayout = rowObj.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 14f;
        rowLayout.padding = new RectOffset(6, 6, 3, 3);
        rowLayout.childAlignment = TextAnchor.UpperLeft;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = true;

        // Лява дискретна хронологична линия с маркер — винаги извън хартиената карта и текста.
        var railObj = new GameObject("TimelineRail", typeof(RectTransform));
        railObj.transform.SetParent(rowObj.transform, false);
        LayoutElement railElement = railObj.AddComponent<LayoutElement>();
        railElement.minWidth = 26f;
        railElement.preferredWidth = 26f;
        railElement.flexibleWidth = 0f;

        RectTransform lineRect = PlayerNoteView.Rect(railObj.transform, "VerticalLine",
            new Vector2(.5f, 0f), new Vector2(.5f, 1f));
        lineRect.anchoredPosition = Vector2.zero;
        lineRect.sizeDelta = new Vector2(2f, 0f);
        Image lineImg = lineRect.gameObject.AddComponent<Image>();
        lineImg.color = EvidenceBoardVisuals.BrassHairline;
        lineImg.raycastTarget = false;

        RectTransform nodeRect = PlayerNoteView.Rect(railObj.transform, "Node",
            new Vector2(.5f, 1f), new Vector2(.5f, 1f));
        nodeRect.anchoredPosition = new Vector2(0f, -24f);
        nodeRect.sizeDelta = new Vector2(10f, 10f);
        Image nodeImg = nodeRect.gameObject.AddComponent<Image>();
        nodeImg.color = model.HasTime ? EvidenceBoardVisuals.BrassAccent : EvidenceBoardVisuals.MutedLabelText;
        nodeImg.raycastTarget = false;

        // Хартиена карта за събитието (без вътрешна хоризонтална линия, която да пресича текста).
        var cardObj = new GameObject("EventCard", typeof(RectTransform));
        cardObj.transform.SetParent(rowObj.transform, false);
        LayoutElement cardElement = cardObj.AddComponent<LayoutElement>();
        cardElement.minWidth = 0f;
        cardElement.preferredWidth = 0f;
        cardElement.flexibleWidth = 1f;

        Image cardBg = cardObj.AddComponent<Image>();
        cardBg.sprite = EvidenceBoardVisuals.TimelineCardPaperSprite;
        cardBg.color = model.HasTime
            ? new Color(0.98f, 0.96f, 0.90f, 1f)
            : new Color(0.93f, 0.90f, 0.83f, 1f);
        cardBg.raycastTarget = true;

        Shadow shadow = cardObj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.34f);
        shadow.effectDistance = new Vector2(0f, -3f);

        Outline outline = cardObj.AddComponent<Outline>();
        outline.effectColor = EvidenceBoardVisuals.BrassAccent;
        outline.effectDistance = new Vector2(2.5f, -2.5f);
        outline.enabled = false;

        VerticalLayoutGroup cardLayout = cardObj.AddComponent<VerticalLayoutGroup>();
        cardLayout.padding = new RectOffset(16, 16, 12, 12);
        cardLayout.spacing = 8f;
        cardLayout.childAlignment = TextAnchor.UpperLeft;
        cardLayout.childControlWidth = true;
        cardLayout.childControlHeight = true;
        cardLayout.childForceExpandWidth = true;
        cardLayout.childForceExpandHeight = false;

        // Горен ред: час + заглавие + бутони за редакция и изтриване.
        var headerObj = new GameObject("HeaderRow", typeof(RectTransform));
        headerObj.transform.SetParent(cardObj.transform, false);
        HorizontalLayoutGroup headerLayout = headerObj.AddComponent<HorizontalLayoutGroup>();
        headerLayout.spacing = 10f;
        headerLayout.childAlignment = TextAnchor.UpperLeft;
        headerLayout.childControlWidth = true;
        headerLayout.childControlHeight = true;
        headerLayout.childForceExpandWidth = false;
        headerLayout.childForceExpandHeight = false;

        // Времеви маркер (HH:mm или "Без час").
        var timeObj = new GameObject("EventTime", typeof(RectTransform));
        timeObj.transform.SetParent(headerObj.transform, false);
        LayoutElement timeElement = timeObj.AddComponent<LayoutElement>();
        timeElement.minWidth = 78f;
        timeElement.preferredWidth = 78f;
        timeElement.flexibleWidth = 0f;
        timeElement.minHeight = 28f;
        timeElement.preferredHeight = 28f;
        Image timeBg = timeObj.AddComponent<Image>();
        timeBg.sprite = EvidenceBoardVisuals.ButtonSprite;
        timeBg.type = Image.Type.Sliced;
        timeBg.color = model.HasTime
            ? new Color(0.24f, 0.18f, 0.13f, 0.92f)
            : new Color(0.32f, 0.27f, 0.21f, 0.78f);
        timeBg.raycastTarget = false;
        TMP_Text timeLabel = PlayerNoteView.Label(timeObj.transform, "Text",
            model.HasTime ? model.Time : "Без час", font, 14.5f, TextAlignmentOptions.Center);
        timeLabel.color = EvidenceBoardVisuals.ParchmentText;
        timeLabel.fontStyle = FontStyles.Bold;
        timeLabel.textWrappingMode = TextWrappingModes.NoWrap;

        // Заглавие на събитието.
        var titleObj = new GameObject("EventTitle", typeof(RectTransform));
        titleObj.transform.SetParent(headerObj.transform, false);
        LayoutElement titleElement = titleObj.AddComponent<LayoutElement>();
        titleElement.minWidth = 0f;
        titleElement.preferredWidth = 0f;
        titleElement.flexibleWidth = 1f;
        titleElement.minHeight = 28f;
        TMP_Text titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.font = font;
        titleText.fontSize = 18f;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.TopLeft;
        titleText.margin = new Vector4(0f, 3f, 0f, 0f);
        titleText.color = EvidenceBoardVisuals.PaperInk;
        titleText.richText = false;
        titleText.textWrappingMode = TextWrappingModes.Normal;
        titleText.raycastTarget = false;
        titleText.text = model.Title;

        TimelineEventView view = rowObj.AddComponent<TimelineEventView>();
        view.model = model;
        view.selectionOutline = outline;
        view.editRequested = onEditRequested;
        view.deleteRequested = onDeleteRequested;

        view.editButton = CreateActionButton(headerObj.transform, "EditTimelineEventButton", "Редактирай",
            104f, 28f, font, 13.5f, EvidenceBoardVisuals.ButtonStyle.CorkControl, view.HandleEditClicked);
        view.deleteButton = CreateActionButton(headerObj.transform, "DeleteTimelineEventButton", "Изтрий",
            74f, 28f, font, 13.5f, EvidenceBoardVisuals.ButtonStyle.CaseMutedDanger, view.HandleDeleteClicked);

        // Описание на играча (ако е попълнено).
        if (!string.IsNullOrEmpty(model.Description))
        {
            var descObj = new GameObject("EventDescription", typeof(RectTransform));
            descObj.transform.SetParent(cardObj.transform, false);
            LayoutElement descElement = descObj.AddComponent<LayoutElement>();
            descElement.minWidth = 0f;
            descElement.flexibleWidth = 1f;
            TMP_Text descText = descObj.AddComponent<TextMeshProUGUI>();
            descText.font = font;
            descText.fontSize = 16f;
            descText.alignment = TextAlignmentOptions.TopLeft;
            descText.color = new Color(0.18f, 0.14f, 0.10f, 0.96f);
            descText.richText = false;
            descText.lineSpacing = 4f;
            descText.textWrappingMode = TextWrappingModes.Normal;
            descText.raycastTarget = false;
            descText.text = model.Description;
        }

        return view;
    }

    public void SetSelected(bool selected)
    {
        if (selectionOutline != null)
            selectionOutline.enabled = selected;
    }

    private void HandleEditClicked() => editRequested?.Invoke(this);

    private void HandleDeleteClicked() => deleteRequested?.Invoke(this);

    private void OnDestroy()
    {
        if (editButton != null) editButton.onClick.RemoveListener(HandleEditClicked);
        if (deleteButton != null) deleteButton.onClick.RemoveListener(HandleDeleteClicked);
    }

    private static Button CreateActionButton(Transform parent, string name, string title,
        float width, float height, TMP_FontAsset font, float fontSize,
        EvidenceBoardVisuals.ButtonStyle style, UnityEngine.Events.UnityAction click)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        LayoutElement layout = obj.AddComponent<LayoutElement>();
        layout.minWidth = width;
        layout.preferredWidth = width;
        layout.flexibleWidth = 0f;
        layout.minHeight = height;
        layout.preferredHeight = height;

        Image img = obj.AddComponent<Image>();
        Button btn = obj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.navigation = new Navigation { mode = Navigation.Mode.None };
        btn.onClick.AddListener(click);

        TMP_Text label = PlayerNoteView.Label(obj.transform, "Label", title, font, fontSize, TextAlignmentOptions.Center);
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
        EvidenceBoardVisuals.ApplyButtonStyle(btn, img, label, style);
        return btn;
    }
}
