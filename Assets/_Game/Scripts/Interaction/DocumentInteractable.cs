using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DocumentInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private FirstPersonController playerController;
    [SerializeField] private float inspectionDistance = 0.6f;
    [SerializeField] private float verticalOffset;
    [SerializeField] private Vector3 inspectionRotation;
    [SerializeField] private string displayName = "Документ";
    [SerializeField, TextArea(2, 6)] private string documentText;
    [SerializeField] private TMP_FontAsset inspectionFont;

    public string DisplayName => displayName;
    public string ActionLabel => "Разгледай";
    private InteractionModeController ModeController => playerController != null
        ? playerController.GetComponent<PlayerInteraction>()?.ModeController : null;
    public bool CanInteract => isActiveAndEnabled && playerCamera != null && playerController != null
        && ModeController != null && ModeController.CanEnter(this);

    private Transform originalParent;
    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;
    private Vector3 originalLocalScale;
    private Vector3 originalWorldPosition;
    private Quaternion originalWorldRotation;
    private GameObject readingOverlay;

    public bool IsInspecting { get; private set; }

    public void Interact()
    {
        if (IsInspecting)
        {
            CloseInspection();
            return;
        }

        OpenInspection();
    }

    private void OpenInspection()
    {
        if (playerCamera == null || playerController == null)
        {
            Debug.LogError("DocumentInteractable няма зададени Player Camera и Player Controller", this);
            return;
        }

        if (ModeController == null || !ModeController.TryEnter(this, InteractionModeController.Mode.Inspection))
            return;

        // пази старите стойности
        originalParent = transform.parent;
        originalLocalPosition = transform.localPosition;
        originalLocalRotation = transform.localRotation;
        originalLocalScale = transform.localScale;
        originalWorldPosition = transform.position;
        originalWorldRotation = transform.rotation;

        // мести документа пред камерата
        transform.SetParent(playerCamera.transform, false);
        transform.localPosition = new Vector3(0f, verticalOffset, inspectionDistance);
        transform.localRotation = Quaternion.Euler(inspectionRotation);

        IsInspecting = true;
        ShowReadingOverlay();
    }

    // PROTOTYPE CODE: четим препис на един физически документ, без deduction или progress state.
    private void ShowReadingOverlay()
    {
        if (string.IsNullOrWhiteSpace(documentText))
            return;

        readingOverlay = new GameObject("DocumentReadingOverlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = readingOverlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        CanvasScaler scaler = readingOverlay.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject panel = new GameObject("ReadingPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(readingOverlay.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.56f, 0.2f);
        panelRect.anchorMax = new Vector2(0.94f, 0.8f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        Image background = panel.GetComponent<Image>();
        background.color = new Color(0.06f, 0.08f, 0.1f, 0.94f);
        background.raycastTarget = false;

        GameObject label = new GameObject("DocumentText", typeof(RectTransform), typeof(TextMeshProUGUI));
        label.transform.SetParent(panel.transform, false);
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(30f, 90f);
        labelRect.offsetMax = new Vector2(-30f, -30f);
        TextMeshProUGUI text = label.GetComponent<TextMeshProUGUI>();
        text.font = inspectionFont != null ? inspectionFont : TMP_Settings.defaultFontAsset;
        text.fontSize = 30f;
        text.color = new Color(0.91f, 0.9f, 0.78f);
        text.alignment = TextAlignmentOptions.TopLeft;
        text.raycastTarget = false;
        text.text = documentText;

        // Подсказката е управление, а не част от преписа на документа.
        GameObject hint = new GameObject("CloseHint", typeof(RectTransform), typeof(TextMeshProUGUI));
        hint.transform.SetParent(panel.transform, false);
        RectTransform hintRect = hint.GetComponent<RectTransform>();
        hintRect.anchorMin = Vector2.zero;
        hintRect.anchorMax = new Vector2(1f, 0f);
        hintRect.pivot = new Vector2(0.5f, 0f);
        hintRect.offsetMin = new Vector2(30f, 24f);
        hintRect.offsetMax = new Vector2(-30f, 70f);
        TextMeshProUGUI hintText = hint.GetComponent<TextMeshProUGUI>();
        hintText.font = text.font;
        hintText.fontSize = 24f;
        hintText.color = text.color;
        hintText.raycastTarget = false;
        PlayerInputReader reader = playerController.GetComponent<PlayerInputReader>();
        hintText.text = $"[{reader.GetInteractBindingLabel()}] Върни документа";
    }

    private void CloseInspection()
    {
        if (readingOverlay != null)
        {
            Destroy(readingOverlay);
            readingOverlay = null;
        }

        // връща документа
        if (originalParent != null)
        {
            transform.SetParent(originalParent, false);
            transform.localPosition = originalLocalPosition;
            transform.localRotation = originalLocalRotation;
        }
        else
        {
            transform.SetParent(null, true);
            transform.position = originalWorldPosition;
            transform.rotation = originalWorldRotation;
        }
        transform.localScale = originalLocalScale;

        IsInspecting = false;
        ModeController?.TryExit(this);
    }

    private void OnDisable()
    {
        if (IsInspecting)
            CloseInspection();
    }
}
