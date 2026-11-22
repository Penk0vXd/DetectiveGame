using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Един лист редактира, мести и заявява изтриване само за своя PlayerNote. State-ът живее извън UI обекта.
public sealed class PlayerNoteView : MonoBehaviour
{
    private const float SurfaceMargin = .006f;
    private static readonly Vector2 PaperHalfExtent = new Vector2(.234f, .230f);
    private static readonly Vector2 RestingShadowOffsetMin = new Vector2(-4f, -10f);
    private static readonly Vector2 RestingShadowOffsetMax = new Vector2(10f, 4f);
    private static readonly Vector2 LiftedShadowOffsetMin = new Vector2(-8f, -18f);
    private static readonly Vector2 LiftedShadowOffsetMax = new Vector2(16f, 6f);

    private PlayerNote note;
    private TMP_InputField input;
    private RectTransform rootRect;
    private RectTransform surfaceRect;
    private RectTransform shadowRect;
    private Image shadowImage;
    private Button deleteButton;
    private Action<PlayerNoteView> deleteRequested;
    private bool isDragging;
    private Vector2 dragOffsetNormalized;

    public string NoteId => note.Id;

    public static PlayerNoteView Create(RectTransform parent, PlayerNote note, TMP_FontAsset font,
        Action<PlayerNoteView> onDeleteRequested = null)
    {
        Vector2 position = ClampPosition(new Vector2(note.NormalizedX, note.NormalizedY));
        RectTransform root = Rect(parent, "PlayerNote-" + note.Id,
            position - PaperHalfExtent, position + PaperHalfExtent);

        // Мека сянка под хартията за реалистично отделяне от корка.
        RectTransform shadowRect = Rect(root, "DropShadow", Vector2.zero, Vector2.one);
        shadowRect.offsetMin = RestingShadowOffsetMin;
        shadowRect.offsetMax = RestingShadowOffsetMax;
        Image shadowImage = shadowRect.gameObject.AddComponent<Image>();
        shadowImage.sprite = EvidenceBoardVisuals.SoftShadowSprite;
        shadowImage.type = Image.Type.Sliced;
        shadowImage.color = Color.white;
        shadowImage.raycastTarget = false;

        // Топъл кремав лист с леко износен вид и деликатни редове.
        RectTransform sheetRect = Rect(root, "PaperSheet", Vector2.zero, Vector2.one);
        Image sheetImage = sheetRect.gameObject.AddComponent<Image>();
        sheetImage.sprite = EvidenceBoardVisuals.PaperSheetSprite;
        sheetImage.color = EvidenceBoardVisuals.GetPaperTint(note.Id);
        sheetImage.raycastTarget = true;

        // Вътрешни отстояния с чисто пространство под кабарчето и около ръбовете.
        RectTransform viewport = Rect(root, "TextViewport", new Vector2(.075f, .065f), new Vector2(.925f, .835f));
        viewport.gameObject.AddComponent<RectMask2D>();
        TMP_Text text = Label(viewport, "Text", "", font, 20f, TextAlignmentOptions.TopLeft);
        text.lineSpacing = 6f;
        text.textWrappingMode = TextWrappingModes.Normal;

        TMP_Text placeholder = Label(viewport, "Placeholder", "Запишете бележка...", font, 20f, TextAlignmentOptions.TopLeft);
        placeholder.color = EvidenceBoardVisuals.PaperPlaceholder;
        placeholder.fontStyle = FontStyles.Italic;
        placeholder.lineSpacing = 6f;
        placeholder.textWrappingMode = TextWrappingModes.Normal;

        TMP_InputField input = root.gameObject.AddComponent<TMP_InputField>();
        input.targetGraphic = sheetImage;
        input.textViewport = viewport;
        input.textComponent = text;
        input.placeholder = placeholder;
        input.lineType = TMP_InputField.LineType.MultiLineNewline;
        input.richText = false;
        input.restoreOriginalTextOnEscape = false;
        input.onFocusSelectAll = false;
        input.customCaretColor = true;
        input.caretColor = EvidenceBoardVisuals.PaperInk;
        input.selectionColor = EvidenceBoardVisuals.PaperSelection;
        input.navigation = new Navigation { mode = Navigation.Mode.None };

        ColorBlock colors = input.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.03f, 1.02f, 0.99f, 1f);
        colors.pressedColor = new Color(0.97f, 0.95f, 0.92f, 1f);
        colors.selectedColor = new Color(1.04f, 1.03f, 1.00f, 1f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        input.colors = colors;

        input.SetTextWithoutNotify(note.Text);

        PlayerNoteView view = root.gameObject.AddComponent<PlayerNoteView>();
        view.note = note;
        view.input = input;
        view.rootRect = root;
        view.surfaceRect = parent;
        view.shadowRect = shadowRect;
        view.shadowImage = shadowImage;
        view.deleteRequested = onDeleteRequested;

        // Горна зона около кабарчето за свободно влачене без активиране на текстовото поле.
        RectTransform dragZone = Rect(root, "DragHandle", new Vector2(0f, .835f), new Vector2(.85f, 1f));
        Image dragImage = dragZone.gameObject.AddComponent<Image>();
        dragImage.color = Color.clear;
        dragImage.raycastTarget = true;
        dragZone.gameObject.AddComponent<NoteDragHandle>().Initialize(view);

        // Реалистично кръгло кабарче със собствена сянка и отблясък.
        RectTransform pin = Rect(root, "Pin", new Vector2(.50f, .922f), new Vector2(.50f, .922f));
        pin.sizeDelta = new Vector2(28f, 28f);
        Image pinImage = pin.gameObject.AddComponent<Image>();
        pinImage.sprite = EvidenceBoardVisuals.PushpinSprite;
        pinImage.color = EvidenceBoardVisuals.GetPinTint(note.Id);
        pinImage.preserveAspect = true;
        pinImage.raycastTarget = false;

        // Малък, ненатрапчив бутон за изтриване в горния десен ъгъл на листа.
        view.deleteButton = CreateDeleteButton(root, font, view.RequestDelete);

        input.onValueChanged.AddListener(view.WriteText);
        return view;
    }

    public void Focus() => input.ActivateInputField();

    public void FlushPendingText()
    {
        if (input != null && note != null)
            note.SetText(input.text);
    }

    public void ApplyNormalizedPosition(float normalizedX, float normalizedY)
    {
        Vector2 clamped = ClampPosition(new Vector2(normalizedX, normalizedY));
        note.SetPosition(clamped.x, clamped.y);
        rootRect.anchorMin = clamped - PaperHalfExtent;
        rootRect.anchorMax = clamped + PaperHalfExtent;
        rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;
    }

    private void BeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || surfaceRect == null) return;
        if (!TryGetNormalizedPointer(eventData, out Vector2 pointerNormalized)) return;

        isDragging = true;
        dragOffsetNormalized = new Vector2(note.NormalizedX, note.NormalizedY) - pointerNormalized;
        if (input != null && input.isFocused)
            input.DeactivateInputField();
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

    private static Vector2 ClampPosition(Vector2 position)
    {
        float minX = PaperHalfExtent.x + SurfaceMargin;
        float maxX = 1f - PaperHalfExtent.x - SurfaceMargin;
        float minY = PaperHalfExtent.y + SurfaceMargin;
        float maxY = 1f - PaperHalfExtent.y - SurfaceMargin;
        return new Vector2(Mathf.Clamp(position.x, minX, maxX), Mathf.Clamp(position.y, minY, maxY));
    }

    private void RequestDelete() => deleteRequested?.Invoke(this);

    private void WriteText(string value) => note.SetText(value);

    private void OnDisable() => EndDrag();

    private void OnDestroy()
    {
        if (input != null) input.onValueChanged.RemoveListener(WriteText);
        if (deleteButton != null) deleteButton.onClick.RemoveListener(RequestDelete);
    }

    private static Button CreateDeleteButton(RectTransform parent, TMP_FontAsset font, UnityEngine.Events.UnityAction click)
    {
        RectTransform rect = Rect(parent, "DeleteNoteButton", new Vector2(.865f, .862f), new Vector2(.968f, .966f));
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = EvidenceBoardVisuals.ButtonSprite;
        image.type = Image.Type.Sliced;
        image.color = Color.white;

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.onClick.AddListener(click);

        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.35f, 0.27f, 0.19f, 0.18f);
        colors.highlightedColor = new Color(0.60f, 0.21f, 0.17f, 0.85f);
        colors.pressedColor = new Color(0.45f, 0.15f, 0.13f, 0.95f);
        colors.selectedColor = colors.highlightedColor;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        TMP_Text label = Label(rect, "Label", "X", font, 14f, TextAlignmentOptions.Center);
        label.color = new Color(0.24f, 0.18f, 0.13f, 0.88f);
        return button;
    }

    internal static RectTransform Rect(Transform parent, string name, Vector2 min, Vector2 max)
    {
        var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rect.SetParent(parent, false);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    internal static TMP_Text Label(Transform parent, string name, string value, TMP_FontAsset font,
        float size, TextAlignmentOptions alignment)
    {
        TMP_Text label = Rect(parent, name, Vector2.zero, Vector2.one).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.fontSize = size;
        label.alignment = alignment;
        label.color = EvidenceBoardVisuals.PaperInk;
        label.richText = false;
        label.raycastTarget = false;
        label.text = value;
        return label;
    }

    private sealed class NoteDragHandle : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private PlayerNoteView owner;

        public void Initialize(PlayerNoteView owner) => this.owner = owner;

        // Поглъща клика в горната зона, за да не активира TMP_InputField и да не мести бележката при обикновен клик.
        public void OnPointerDown(PointerEventData eventData) { }

        public void OnBeginDrag(PointerEventData eventData) => owner?.BeginDrag(eventData);

        public void OnDrag(PointerEventData eventData) => owner?.UpdateDrag(eventData);

        public void OnEndDrag(PointerEventData eventData) => owner?.EndDrag();
    }
}
