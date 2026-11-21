using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Един лист редактира само своя PlayerNote. State-ът живее извън UI обекта.
public sealed class PlayerNoteView : MonoBehaviour
{
    private PlayerNote note;
    private TMP_InputField input;

    public string NoteId => note.Id;

    public static PlayerNoteView Create(RectTransform parent, PlayerNote note, TMP_FontAsset font)
    {
        Vector2 position = new Vector2(note.NormalizedX, note.NormalizedY);
        RectTransform paper = Rect(parent, "PlayerNote-" + note.Id,
            position - new Vector2(.225f, .215f), position + new Vector2(.225f, .215f));
        Image image = paper.gameObject.AddComponent<Image>();
        image.color = new Color(.94f, .89f, .70f);
        Shadow shadow = paper.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(.08f, .05f, .02f, .35f);
        shadow.effectDistance = new Vector2(3f, -4f);
        Outline outline = paper.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(.38f, .30f, .19f, .35f);
        outline.effectDistance = new Vector2(1f, -1f);

        RectTransform pin = Rect(paper, "Pin", new Vector2(.48f, .94f), new Vector2(.52f, .98f));
        Image pinImage = pin.gameObject.AddComponent<Image>();
        pinImage.color = new Color(.35f, .25f, .18f);
        pinImage.raycastTarget = false;

        RectTransform viewport = Rect(paper, "TextViewport", new Vector2(.06f, .05f), new Vector2(.94f, .90f));
        viewport.gameObject.AddComponent<RectMask2D>();
        TMP_Text text = Label(viewport, "Text", "", font, 24, TextAlignmentOptions.TopLeft);
        TMP_Text placeholder = Label(viewport, "Placeholder", "Напишете бележка", font, 24, TextAlignmentOptions.TopLeft);
        placeholder.color = new Color(.18f, .14f, .09f);
        TMP_InputField input = paper.gameObject.AddComponent<TMP_InputField>();
        input.targetGraphic = image;
        input.textViewport = viewport;
        input.textComponent = text;
        input.placeholder = placeholder;
        input.lineType = TMP_InputField.LineType.MultiLineNewline;
        input.richText = false;
        input.restoreOriginalTextOnEscape = false;
        input.onFocusSelectAll = false;
        input.customCaretColor = true;
        input.caretColor = new Color(.06f, .045f, .025f);
        input.selectionColor = new Color(.45f, .35f, .18f, .30f);
        input.navigation = new Navigation { mode = Navigation.Mode.None };
        input.SetTextWithoutNotify(note.Text);

        PlayerNoteView view = paper.gameObject.AddComponent<PlayerNoteView>();
        view.note = note;
        view.input = input;
        input.onValueChanged.AddListener(view.WriteText);
        return view;
    }

    public void Focus() => input.ActivateInputField();

    private void WriteText(string value) => note.SetText(value);

    private void OnDestroy()
    {
        if (input != null) input.onValueChanged.RemoveListener(WriteText);
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
        label.color = new Color(.06f, .045f, .025f);
        label.richText = false;
        label.raycastTarget = false;
        label.text = value;
        return label;
    }
}
