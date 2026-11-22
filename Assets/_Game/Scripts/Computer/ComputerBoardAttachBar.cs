using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Малък, неутрален контрол в стил 2003 за ръчно добавяне на копие към Evidence Board.
public sealed class ComputerBoardAttachBar : MonoBehaviour
{
    public const string AttachButtonLabel = "Добави копие към дъската";
    public const string AddedStatusMessage = "Копието е добавено към дъската.";
    public const string AlreadyAttachedStatusMessage = "Вече е на дъската";

    private static readonly Color ButtonFaceColor = new Color(0.753f, 0.753f, 0.753f, 1f);
    private static readonly Color ButtonHighlightColor = new Color(0.835f, 0.835f, 0.835f, 1f);
    private static readonly Color ButtonPressedColor = new Color(0.64f, 0.64f, 0.64f, 1f);
    private static readonly Color DarkTextColor = Color.black;
    private static readonly Color NeutralStatusColor = new Color(0.18f, 0.22f, 0.26f, 1f);

    private Button attachButton;
    private TMP_Text statusText;
    private Action clickHandler;

    public Button AttachButton => attachButton;
    public TMP_Text StatusText => statusText;

    public static ComputerBoardAttachBar Create(Transform parent, TMP_FontAsset font, string buttonName, Action onClick)
    {
        var rootGo = new GameObject("BoardAttachBar", typeof(RectTransform), typeof(LayoutElement), typeof(HorizontalLayoutGroup));
        RectTransform root = rootGo.GetComponent<RectTransform>();
        root.SetParent(parent, false);
        root.localScale = Vector3.one;

        LayoutElement rootLayout = rootGo.GetComponent<LayoutElement>();
        rootLayout.minHeight = 36f;
        rootLayout.preferredHeight = 38f;
        rootLayout.flexibleWidth = 1f;

        HorizontalLayoutGroup layout = rootGo.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(0, 0, 2, 2);
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var buttonGo = new GameObject(buttonName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
        RectTransform buttonRect = buttonGo.GetComponent<RectTransform>();
        buttonRect.SetParent(root, false);
        buttonRect.localScale = Vector3.one;

        Image buttonImage = buttonGo.GetComponent<Image>();
        buttonImage.color = Color.white;
        Outline outline = buttonGo.AddComponent<Outline>();
        outline.effectColor = new Color(0.25f, 0.25f, 0.25f, 1f);
        outline.effectDistance = new Vector2(1f, -1f);

        LayoutElement buttonLayout = buttonGo.GetComponent<LayoutElement>();
        buttonLayout.minWidth = 240f;
        buttonLayout.preferredWidth = 270f;
        buttonLayout.minHeight = 32f;
        buttonLayout.preferredHeight = 34f;

        Button button = buttonGo.GetComponent<Button>();
        button.targetGraphic = buttonImage;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        ColorBlock colors = button.colors;
        colors.normalColor = ButtonFaceColor;
        colors.highlightedColor = ButtonHighlightColor;
        colors.pressedColor = ButtonPressedColor;
        colors.selectedColor = ButtonHighlightColor;
        colors.disabledColor = new Color(0.753f, 0.753f, 0.753f, 0.55f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.06f;
        button.colors = colors;

        var labelGo = new GameObject("LabelText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.SetParent(buttonRect, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(8f, 2f);
        labelRect.offsetMax = new Vector2(-8f, -2f);
        TMP_Text label = labelGo.GetComponent<TMP_Text>();
        label.font = font;
        label.fontSize = 15f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = DarkTextColor;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.raycastTarget = false;
        label.text = AttachButtonLabel;

        var statusGo = new GameObject("BoardAttachStatusText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
        RectTransform statusRect = statusGo.GetComponent<RectTransform>();
        statusRect.SetParent(root, false);
        statusRect.localScale = Vector3.one;
        LayoutElement statusLayout = statusGo.GetComponent<LayoutElement>();
        statusLayout.flexibleWidth = 1f;
        statusLayout.minHeight = 30f;
        statusLayout.preferredHeight = 34f;

        TMP_Text status = statusGo.GetComponent<TMP_Text>();
        status.font = font;
        status.fontSize = 15f;
        status.alignment = TextAlignmentOptions.MidlineLeft;
        status.color = NeutralStatusColor;
        status.textWrappingMode = TextWrappingModes.NoWrap;
        status.overflowMode = TextOverflowModes.Ellipsis;
        status.raycastTarget = false;
        status.text = string.Empty;

        ComputerBoardAttachBar bar = rootGo.AddComponent<ComputerBoardAttachBar>();
        bar.attachButton = button;
        bar.statusText = status;
        bar.clickHandler = onClick;
        button.onClick.AddListener(bar.HandleClick);
        bar.SetHidden();
        return bar;
    }

    public void SetHidden()
    {
        if (statusText != null) statusText.text = string.Empty;
        gameObject.SetActive(false);
    }

    public void ShowReady(bool alreadyAttached)
    {
        gameObject.SetActive(true);
        if (attachButton != null) attachButton.interactable = true;
        if (statusText != null)
            statusText.text = alreadyAttached ? AlreadyAttachedStatusMessage : string.Empty;
    }

    public void ShowStatus(string message)
    {
        gameObject.SetActive(true);
        if (statusText != null)
            statusText.text = message ?? string.Empty;
    }

    private void HandleClick() => clickHandler?.Invoke();

    private void OnDestroy()
    {
        if (attachButton != null)
            attachButton.onClick.RemoveListener(HandleClick);
    }
}
