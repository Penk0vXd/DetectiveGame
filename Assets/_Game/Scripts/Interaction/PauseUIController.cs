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

    private void Awake()
    {
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
        if (!referencesValid)
            return;

        resumeButton.onClick.RemoveListener(Resume);
        exitButton.onClick.RemoveListener(ShowExitConfirmation);
        cancelExitButton.onClick.RemoveListener(ShowMain);
        confirmExitButton.onClick.RemoveListener(ConfirmExit);
    }
}
