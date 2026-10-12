using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// PROTOTYPE CODE: показва pause и изхода; save/settings се добавят отделно.
[DefaultExecutionOrder(-100)]
public class PauseUIController : MonoBehaviour
{
    [SerializeField] private PlayerInputReader inputReader;
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private GameObject pauseRoot;
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject exitConfirmationPanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private Button cancelExitButton;
    [SerializeField] private Button confirmExitButton;

    private bool referencesValid;
    private GameObject selectionBeforePause;
    private GameObject selectionToRestore;
    private bool restoreSelectionNextFrame;
    private GameObject runtimeRoot;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureOfficePauseController()
    {
        PlayerInteraction player = FindAnyObjectByType<PlayerInteraction>();
        if (player != null && player.GetComponent<PauseUIController>() == null)
            player.gameObject.AddComponent<PauseUIController>();
    }

    private void Awake()
    {
        if (inputReader == null)
            inputReader = GetComponent<PlayerInputReader>();
        if (playerInteraction == null)
            playerInteraction = GetComponent<PlayerInteraction>();

        // Старите сцени нямат serialized PauseCanvas. Изграждаме същия малък
        // екран в runtime, за да работи Esc и в build без Editor setup стъпка.
        if (pauseRoot == null || mainPanel == null || exitConfirmationPanel == null
            || resumeButton == null || exitButton == null || cancelExitButton == null || confirmExitButton == null)
        {
            try
            {
                PauseUIRuntimeView view = PauseUIRuntimeView.Create();
                runtimeRoot = pauseRoot = view.Root;
                mainPanel = view.MainPanel;
                exitConfirmationPanel = view.ExitConfirmationPanel;
                resumeButton = view.ResumeButton;
                exitButton = view.ExitButton;
                cancelExitButton = view.CancelExitButton;
                confirmExitButton = view.ConfirmExitButton;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }

        referencesValid = inputReader != null && playerInteraction != null && pauseRoot != null
            && mainPanel != null && exitConfirmationPanel != null && resumeButton != null
            && exitButton != null && cancelExitButton != null && confirmExitButton != null;
        if (!referencesValid)
        {
            Debug.LogError("PauseUIController няма зададени нужните references", this);
            enabled = false;
            return;
        }

        resumeButton.onClick.AddListener(Resume);
        exitButton.onClick.AddListener(ShowExitConfirmation);
        cancelExitButton.onClick.AddListener(ShowMain);
        confirmExitButton.onClick.AddListener(ConfirmExit);
        pauseRoot.SetActive(false);
    }

    private void Update()
    {
        if (restoreSelectionNextFrame)
        {
            restoreSelectionNextFrame = false;
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(selectionToRestore != null && selectionToRestore.activeInHierarchy
                    ? selectionToRestore : null);
            selectionToRestore = null;
        }

        if (referencesValid && playerInteraction.ModeController.IsPaused && !pauseRoot.activeInHierarchy)
        {
            Resume();
            return;
        }

        if (!referencesValid || !inputReader.ReadPause())
            return;

        if (playerInteraction.ModeController.IsPaused)
            Resume();
        else
            Pause();
    }

    private void Pause()
    {
        if (!playerInteraction.ModeController.TryPause())
            return;

        selectionBeforePause = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        ShowMain();
        pauseRoot.SetActive(true);
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
    }

    public void Resume()
    {
        if (!playerInteraction.ModeController.IsPaused)
            return;

        pauseRoot.SetActive(false);
        playerInteraction.ModeController.TryResume();
        // Възстановява focus чак в следващия кадър: същият Esc не стига до
        // подлежащото поле/бутон като UI Cancel.
        selectionToRestore = selectionBeforePause;
        restoreSelectionNextFrame = true;
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
        selectionBeforePause = null;
    }

    public void ShowMain()
    {
        if (!playerInteraction.ModeController.IsPaused)
            return;

        mainPanel.SetActive(true);
        exitConfirmationPanel.SetActive(false);
        if (pauseRoot.activeInHierarchy && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
    }

    public void ShowExitConfirmation()
    {
        if (!playerInteraction.ModeController.IsPaused)
            return;

        mainPanel.SetActive(false);
        exitConfirmationPanel.SetActive(true);
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(cancelExitButton.gameObject);
    }

    private void ConfirmExit()
    {
        if (playerInteraction.ModeController.IsPaused && exitConfirmationPanel.activeSelf)
            Application.Quit();
    }

    private void OnDisable()
    {
        if (pauseRoot != null)
            pauseRoot.SetActive(false);
        playerInteraction?.ModeController?.TryResume();
        selectionBeforePause = null;
        selectionToRestore = null;
        restoreSelectionNextFrame = false;
    }

    private void OnDestroy()
    {
        if (referencesValid)
        {
            resumeButton.onClick.RemoveListener(Resume);
            exitButton.onClick.RemoveListener(ShowExitConfirmation);
            cancelExitButton.onClick.RemoveListener(ShowMain);
            confirmExitButton.onClick.RemoveListener(ConfirmExit);
        }
        if (runtimeRoot != null)
            Destroy(runtimeRoot);
    }
}

// PROTOTYPE CODE: view fallback за Office сцени, записани преди PauseCanvas.
internal sealed class PauseUIRuntimeView
{
    public GameObject Root { get; private set; }
    public GameObject MainPanel { get; private set; }
    public GameObject ExitConfirmationPanel { get; private set; }
    public Button ResumeButton { get; private set; }
    public Button ExitButton { get; private set; }
    public Button CancelExitButton { get; private set; }
    public Button ConfirmExitButton { get; private set; }

    public static PauseUIRuntimeView Create()
    {
        InteractionHUD hud = UnityEngine.Object.FindAnyObjectByType<InteractionHUD>();
        TMP_Text hudText = hud != null ? hud.GetComponentInChildren<TMP_Text>(true) : null;
        TMP_FontAsset font = hudText != null ? hudText.font : TMP_Settings.defaultFontAsset;
        if (font == null)
            throw new InvalidOperationException("Pause UI няма достъпен TMP font.");

        var view = new PauseUIRuntimeView();
        view.Root = new GameObject("RuntimePauseCanvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = view.Root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = view.Root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform blocker = Child(view.Root.transform, "Blocker", Vector2.zero, Vector2.zero);
        blocker.anchorMin = Vector2.zero;
        blocker.anchorMax = Vector2.one;
        blocker.offsetMin = blocker.offsetMax = Vector2.zero;
        Image blockerImage = blocker.gameObject.AddComponent<Image>();
        blockerImage.color = new Color(0.015f, 0.02f, 0.025f, 0.92f);
        blockerImage.raycastTarget = true;

        RectTransform main = Child(view.Root.transform, "MainPanel", Vector2.zero, new Vector2(560, 420));
        Panel(main);
        Text(Child(main, "Title", new Vector2(0, 130), new Vector2(500, 65)), font, "ПАУЗА", 36);
        view.ResumeButton = Button(Child(main, "ResumeButton", new Vector2(0, 20), new Vector2(390, 64)), font, "Продължи");
        view.ExitButton = Button(Child(main, "ExitButton", new Vector2(0, -76), new Vector2(390, 64)), font, "Изход от играта");
        view.MainPanel = main.gameObject;

        RectTransform confirm = Child(view.Root.transform, "ExitConfirmationPanel", Vector2.zero, new Vector2(560, 350));
        Panel(confirm);
        Text(Child(confirm, "Title", new Vector2(0, 90), new Vector2(500, 65)), font, "ИЗХОД ОТ ИГРАТА?", 29);
        view.CancelExitButton = Button(Child(confirm, "CancelButton", new Vector2(-105, -70), new Vector2(190, 64)), font, "Откажи");
        view.ConfirmExitButton = Button(Child(confirm, "ConfirmButton", new Vector2(105, -70), new Vector2(190, 64)), font, "Излез");
        confirm.gameObject.SetActive(false);
        view.ExitConfirmationPanel = confirm.gameObject;
        view.Root.SetActive(false);
        return view;
    }

    private static RectTransform Child(Transform parent, string name, Vector2 position, Vector2 size)
    {
        var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static void Panel(RectTransform rect)
    {
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.075f, 0.09f, 0.10f, 0.98f);
        image.raycastTarget = true;
    }

    private static void Text(RectTransform rect, TMP_FontAsset font, string value, float size)
    {
        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.fontSize = size;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0.94f, 0.92f, 0.85f);
        label.richText = false;
        label.raycastTarget = false;
        label.text = value;
    }

    private static Button Button(RectTransform rect, TMP_FontAsset font, string value)
    {
        Image background = rect.gameObject.AddComponent<Image>();
        background.color = new Color(0.15f, 0.19f, 0.19f);
        background.raycastTarget = true;
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        Text(Child(rect, "Label", Vector2.zero, rect.sizeDelta), font, value, 24);
        return button;
    }
}
