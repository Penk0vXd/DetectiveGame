using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Представя едно ръчно прикачено копие (Документ или Снимка) върху корковата дъска.
public sealed class AttachedSourceView : MonoBehaviour
{
    private const float SurfaceMargin = .006f;
    private static readonly Vector2 DocumentHalfExtent = new Vector2(.205f, .215f);
    private static readonly Vector2 PhotoHalfExtent = new Vector2(.205f, .210f);
    private static readonly Vector2 RestingShadowOffsetMin = new Vector2(-4f, -10f);
    private static readonly Vector2 RestingShadowOffsetMax = new Vector2(10f, 4f);
    private static readonly Vector2 LiftedShadowOffsetMin = new Vector2(-8f, -18f);
    private static readonly Vector2 LiftedShadowOffsetMax = new Vector2(16f, 6f);

    private AttachedSource attached;
    private BoardSourceRecord record;
    private RectTransform rootRect;
    private RectTransform surfaceRect;
    private RectTransform shadowRect;
    private Image shadowImage;
    private Outline selectionOutline;
    private Image photoImage;
    private Button openButton;
    private Button removeButton;
    private Vector2 halfExtent;
    private Action<AttachedSourceView> inspectRequested;
    private Action<AttachedSourceView> removeRequested;
    private bool isDragging;
    private bool suppressNextClick;
    private Vector2 dragOffsetNormalized;

    public string AttachmentId => attached.AttachmentId;
    public string SourceId => attached.SourceId;
    public AttachedSourceType SourceType => attached.SourceType;
    public AttachedSource Model => attached;
    public BoardSourceRecord Record => record;
    public Image PhotoImage => photoImage;

    public static AttachedSourceView Create(
        RectTransform parent,
        AttachedSource attached,
        BoardSourceRecord record,
        TMP_FontAsset font,
        Action<AttachedSourceView> onInspectRequested,
        Action<AttachedSourceView> onRemoveRequested)
    {
        Vector2 halfExtent = attached.SourceType == AttachedSourceType.Photo ? PhotoHalfExtent : DocumentHalfExtent;
        Vector2 position = ClampPosition(new Vector2(attached.NormalizedX, attached.NormalizedY), halfExtent);

        RectTransform root = PlayerNoteView.Rect(parent, "AttachedSource-" + attached.AttachmentId,
            position - halfExtent, position + halfExtent);

        // Мека сянка под хартията/снимката.
        RectTransform shadowRect = PlayerNoteView.Rect(root, "DropShadow", Vector2.zero, Vector2.one);
        shadowRect.offsetMin = RestingShadowOffsetMin;
        shadowRect.offsetMax = RestingShadowOffsetMax;
        Image shadowImage = shadowRect.gameObject.AddComponent<Image>();
        shadowImage.sprite = EvidenceBoardVisuals.SoftShadowSprite;
        shadowImage.type = Image.Type.Sliced;
        shadowImage.color = Color.white;
        shadowImage.raycastTarget = false;

        // Основно тяло (клик -> detail view, влачене -> местене по дъската).
        RectTransform cardRect = PlayerNoteView.Rect(root, "OpenSourceButton", Vector2.zero, Vector2.one);
        Image cardImage = cardRect.gameObject.AddComponent<Image>();
        if (attached.SourceType == AttachedSourceType.Photo)
        {
            cardImage.sprite = EvidenceBoardVisuals.PhotoFrameSprite;
            cardImage.type = Image.Type.Sliced;
            cardImage.color = Color.white;
        }
        else
        {
            cardImage.sprite = EvidenceBoardVisuals.PrintoutPaperSprite;
            cardImage.type = Image.Type.Simple;
            cardImage.color = Color.white;
        }
        cardImage.raycastTarget = true;

        Outline selectionOutline = cardRect.gameObject.AddComponent<Outline>();
        selectionOutline.effectColor = EvidenceBoardVisuals.BrassAccent;
        selectionOutline.effectDistance = new Vector2(3f, -3f);
        selectionOutline.enabled = false;

        Button openButton = cardRect.gameObject.AddComponent<Button>();
        openButton.targetGraphic = cardImage;
        openButton.navigation = new Navigation { mode = Navigation.Mode.None };
        ColorBlock openColors = openButton.colors;
        openColors.normalColor = Color.white;
        openColors.highlightedColor = new Color(1.03f, 1.02f, 0.99f, 1f);
        openColors.pressedColor = new Color(0.95f, 0.93f, 0.90f, 1f);
        openColors.selectedColor = openColors.highlightedColor;
        openColors.colorMultiplier = 1f;
        openColors.fadeDuration = 0.08f;
        openButton.colors = openColors;

        AttachedSourceView view = root.gameObject.AddComponent<AttachedSourceView>();
        view.attached = attached;
        view.record = record;
        view.rootRect = root;
        view.surfaceRect = parent;
        view.shadowRect = shadowRect;
        view.shadowImage = shadowImage;
        view.selectionOutline = selectionOutline;
        view.openButton = openButton;
        view.halfExtent = halfExtent;
        view.inspectRequested = onInspectRequested;
        view.removeRequested = onRemoveRequested;

        cardRect.gameObject.AddComponent<SourceDragSurface>().Initialize(view);
        openButton.onClick.AddListener(view.HandleCardClick);

        if (attached.SourceType == AttachedSourceType.Photo)
            view.BuildPhotoContent(cardRect, record, font);
        else
            view.BuildDocumentContent(cardRect, record, font);

        // Кабарче в горната среда.
        RectTransform pin = PlayerNoteView.Rect(root, "Pin", new Vector2(.50f, .935f), new Vector2(.50f, .935f));
        pin.sizeDelta = new Vector2(26f, 26f);
        Image pinImage = pin.gameObject.AddComponent<Image>();
        pinImage.sprite = EvidenceBoardVisuals.PushpinSprite;
        pinImage.color = EvidenceBoardVisuals.GetPinTint("src:" + attached.SourceId);
        pinImage.preserveAspect = true;
        pinImage.raycastTarget = false;

        // Малък бутон за премахване в горния десен ъгъл.
        view.removeButton = CreateRemoveButton(root, font, view.RequestRemove);
        return view;
    }

    private void BuildDocumentContent(RectTransform parent, BoardSourceRecord data, TMP_FontAsset font)
    {
        RectTransform headerRect = PlayerNoteView.Rect(parent, "SourceMeta", new Vector2(.07f, .79f), new Vector2(.84f, .89f));
        string meta = string.IsNullOrWhiteSpace(data.SourceLabel) ? "ДОКУМЕНТ" : "ДОКУМЕНТ • " + data.SourceLabel;
        TMP_Text metaText = CreateCardLabel(headerRect, "Text", meta, font, 12f, TextAlignmentOptions.MidlineLeft);
        metaText.color = new Color(0.32f, 0.25f, 0.18f, 0.95f);
        metaText.fontStyle = FontStyles.UpperCase;
        metaText.textWrappingMode = TextWrappingModes.NoWrap;

        RectTransform titleRect = PlayerNoteView.Rect(parent, "SourceTitle", new Vector2(.07f, .60f), new Vector2(.93f, .76f));
        TMP_Text titleText = CreateCardLabel(titleRect, "Text", data.Title, font, 14.5f, TextAlignmentOptions.TopLeft);
        titleText.fontStyle = FontStyles.Bold;
        titleText.lineSpacing = 2f;

        RectTransform bodyRect = PlayerNoteView.Rect(parent, "SourceExcerpt", new Vector2(.07f, .14f), new Vector2(.93f, .58f));
        TMP_Text bodyText = CreateCardLabel(bodyRect, "Text", data.Body, font, 13f, TextAlignmentOptions.TopLeft);
        bodyText.color = new Color(0.16f, 0.13f, 0.10f, 0.96f);
        bodyText.lineSpacing = 3f;

        RectTransform footerRect = PlayerNoteView.Rect(parent, "SourceFooter", new Vector2(.07f, .04f), new Vector2(.93f, .13f));
        string footer = string.IsNullOrWhiteSpace(data.Date) ? "Кликни за преглед" : data.Date + " • Преглед";
        TMP_Text footerText = CreateCardLabel(footerRect, "Text", footer, font, 11.5f, TextAlignmentOptions.MidlineRight);
        footerText.color = new Color(0.38f, 0.31f, 0.23f, 0.85f);
        footerText.textWrappingMode = TextWrappingModes.NoWrap;
    }

    private void BuildPhotoContent(RectTransform parent, BoardSourceRecord data, TMP_FontAsset font)
    {
        // Тъмна подложка за снимката вътре в бялата/пожълтяла фото рамка.
        RectTransform photoBox = PlayerNoteView.Rect(parent, "PhotoFrameInner", new Vector2(.075f, .25f), new Vector2(.925f, .865f));
        Image backing = photoBox.gameObject.AddComponent<Image>();
        backing.color = new Color(0.11f, 0.12f, 0.13f, 1f);
        backing.raycastTarget = false;

        RectTransform imgRect = PlayerNoteView.Rect(photoBox, "PhotoImage", new Vector2(.03f, .03f), new Vector2(.97f, .97f));
        photoImage = imgRect.gameObject.AddComponent<Image>();
        photoImage.preserveAspect = true;
        photoImage.raycastTarget = false;
        photoImage.sprite = data.Image;
        photoImage.enabled = data.Image != null;

        if (data.Image == null)
        {
            RectTransform emptyRect = PlayerNoteView.Rect(photoBox, "NoImageLabel", new Vector2(.08f, .15f), new Vector2(.92f, .85f));
            TMP_Text emptyText = CreateCardLabel(emptyRect, "Text", "Няма налично изображение", font, 13f, TextAlignmentOptions.Center);
            emptyText.color = new Color(0.75f, 0.73f, 0.69f, 0.85f);
        }

        // Кратък надпис и дата в долната рамка — само ако присъстват в оригиналните данни.
        string caption = data.Title;
        string date = data.Date;
        if (!string.IsNullOrWhiteSpace(caption) && !string.IsNullOrWhiteSpace(date))
        {
            RectTransform titleRect = PlayerNoteView.Rect(parent, "PhotoCaption", new Vector2(.07f, .125f), new Vector2(.93f, .23f));
            TMP_Text captionText = CreateCardLabel(titleRect, "Text", caption, font, 13f, TextAlignmentOptions.Center);
            captionText.fontStyle = FontStyles.Bold;
            captionText.textWrappingMode = TextWrappingModes.NoWrap;

            RectTransform dateRect = PlayerNoteView.Rect(parent, "PhotoDate", new Vector2(.07f, .035f), new Vector2(.93f, .125f));
            TMP_Text dateText = CreateCardLabel(dateRect, "Text", date, font, 11.5f, TextAlignmentOptions.Center);
            dateText.color = new Color(0.35f, 0.30f, 0.24f, 0.90f);
            dateText.textWrappingMode = TextWrappingModes.NoWrap;
        }
        else if (!string.IsNullOrWhiteSpace(caption))
        {
            RectTransform titleRect = PlayerNoteView.Rect(parent, "PhotoCaption", new Vector2(.07f, .06f), new Vector2(.93f, .22f));
            TMP_Text captionText = CreateCardLabel(titleRect, "Text", caption, font, 13f, TextAlignmentOptions.Center);
            captionText.fontStyle = FontStyles.Bold;
            captionText.textWrappingMode = TextWrappingModes.NoWrap;
        }
        else if (!string.IsNullOrWhiteSpace(date))
        {
            RectTransform dateRect = PlayerNoteView.Rect(parent, "PhotoDate", new Vector2(.07f, .06f), new Vector2(.93f, .22f));
            TMP_Text dateText = CreateCardLabel(dateRect, "Text", date, font, 12f, TextAlignmentOptions.Center);
            dateText.color = new Color(0.35f, 0.30f, 0.24f, 0.90f);
            dateText.textWrappingMode = TextWrappingModes.NoWrap;
        }
    }

    public void OpenDetails() => inspectRequested?.Invoke(this);

    public void SetSelected(bool selected)
    {
        if (selectionOutline != null)
            selectionOutline.enabled = selected;
    }

    public void ApplyNormalizedPosition(float normalizedX, float normalizedY)
    {
        Vector2 clamped = ClampPosition(new Vector2(normalizedX, normalizedY), halfExtent);
        attached.SetPosition(clamped.x, clamped.y);
        rootRect.anchorMin = clamped - halfExtent;
        rootRect.anchorMax = clamped + halfExtent;
        rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;
    }

    private void HandleCardClick()
    {
        if (suppressNextClick)
        {
            suppressNextClick = false;
            return;
        }

        inspectRequested?.Invoke(this);
    }

    private void BeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || surfaceRect == null) return;
        if (!TryGetNormalizedPointer(eventData, out Vector2 pointerNormalized)) return;

        isDragging = true;
        suppressNextClick = true;
        dragOffsetNormalized = new Vector2(attached.NormalizedX, attached.NormalizedY) - pointerNormalized;
        rootRect.SetAsLastSibling();
        SetLiftedVisual(true);
    }

    private void UpdateDrag(PointerEventData eventData)
    {
        if (!isDragging || eventData.button != PointerEventData.InputButton.Left || surfaceRect == null) return;
        if (!TryGetNormalizedPointer(eventData, out Vector2 pointerNormalized)) return;

        Vector2 target = pointerNormalized + dragOffsetNormalized;
        ApplyNormalizedPosition(target.x, target.y);
    }

    private void EndDrag()
    {
        if (!isDragging) return;
        isDragging = false;
        SetLiftedVisual(false);
    }

    private bool TryGetNormalizedPointer(PointerEventData eventData, out Vector2 normalized)
    {
        normalized = default;
        if (surfaceRect == null) return false;
        UnityEngine.Rect rect = surfaceRect.rect;
        if (rect.width <= 0.001f || rect.height <= 0.001f) return false;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(surfaceRect, eventData.position,
                eventData.pressEventCamera, out Vector2 localPoint))
            return false;

        normalized = UnityEngine.Rect.PointToNormalized(rect, localPoint);
        return true;
    }

    private void SetLiftedVisual(bool lifted)
    {
        if (shadowRect == null) return;
        shadowRect.offsetMin = lifted ? LiftedShadowOffsetMin : RestingShadowOffsetMin;
        shadowRect.offsetMax = lifted ? LiftedShadowOffsetMax : RestingShadowOffsetMax;
        if (shadowImage != null)
            shadowImage.color = lifted ? new Color(1f, 1f, 1f, 1.15f) : Color.white;
    }

    private static Vector2 ClampPosition(Vector2 position, Vector2 extent)
    {
        float minX = extent.x + SurfaceMargin;
        float maxX = 1f - extent.x - SurfaceMargin;
        float minY = extent.y + SurfaceMargin;
        float maxY = 1f - extent.y - SurfaceMargin;
        return new Vector2(Mathf.Clamp(position.x, minX, maxX), Mathf.Clamp(position.y, minY, maxY));
    }

    private void RequestRemove() => removeRequested?.Invoke(this);

    private void OnDisable()
    {
        EndDrag();
        suppressNextClick = false;
    }

    private void OnDestroy()
    {
        if (openButton != null) openButton.onClick.RemoveListener(HandleCardClick);
        if (removeButton != null) removeButton.onClick.RemoveListener(RequestRemove);
    }

    private static TMP_Text CreateCardLabel(Transform parent, string name, string value, TMP_FontAsset font,
        float fontSize, TextAlignmentOptions alignment)
    {
        TMP_Text label = PlayerNoteView.Label(parent, name, value ?? string.Empty, font, fontSize, alignment);
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Ellipsis;
        return label;
    }

    private static Button CreateRemoveButton(RectTransform parent, TMP_FontAsset font, UnityEngine.Events.UnityAction click)
    {
        RectTransform rect = PlayerNoteView.Rect(parent, "RemoveAttachedSourceButton", new Vector2(.855f, .865f), new Vector2(.968f, .968f));
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = EvidenceBoardVisuals.ButtonSprite;
        image.type = Image.Type.Sliced;
        image.color = Color.white;

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.onClick.AddListener(click);

        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.35f, 0.27f, 0.19f, 0.22f);
        colors.highlightedColor = new Color(0.60f, 0.21f, 0.17f, 0.88f);
        colors.pressedColor = new Color(0.45f, 0.15f, 0.13f, 0.95f);
        colors.selectedColor = colors.highlightedColor;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        TMP_Text label = PlayerNoteView.Label(rect, "Label", "X", font, 13f, TextAlignmentOptions.Center);
        label.color = new Color(0.24f, 0.18f, 0.13f, 0.90f);
        return button;
    }

    private sealed class SourceDragSurface : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private AttachedSourceView owner;

        public void Initialize(AttachedSourceView owner) => this.owner = owner;

        public void OnBeginDrag(PointerEventData eventData) => owner?.BeginDrag(eventData);

        public void OnDrag(PointerEventData eventData) => owner?.UpdateDrag(eventData);

        public void OnEndDrag(PointerEventData eventData) => owner?.EndDrag();
    }
}
