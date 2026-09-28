using System;
using UnityEngine;

public class ComputerUIController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject computerRoot;
    [SerializeField] private GameObject screenOffPanel;
    [SerializeField] private GameObject bootPanel;
    [SerializeField] private GameObject desktopPanel;
    [SerializeField] private GameObject powerMenuPanel;

    [Header("Desktop Applications")]
    [SerializeField] private EmailUIController emailUIController = null;
    [SerializeField] private CaseFilesUIController caseFilesUIController = null;
    [SerializeField] private GameObject databaseWindow = null;
    [SerializeField] private GameObject photosWindow = null;
    [SerializeField] private GameObject forensicsWindow = null;

    private bool referencesValid;

    public bool IsOpen { get; private set; }

    public event Action ExitRequested;

    private void Awake()
    {
        referencesValid = computerRoot != null
            && screenOffPanel != null
            && bootPanel != null
            && desktopPanel != null
            && powerMenuPanel != null;

        if (!referencesValid)
        {
            Debug.LogError("ComputerUIController няма зададени нужните references", this);
        }

        if (computerRoot != null)
        {
            computerRoot.SetActive(false);
        }

        CloseAllDesktopApplications();
        ShowOffScreen();
    }

    private void OnDisable()
    {
        if (!IsOpen || desktopPanel == null)
        {
            return;
        }

        IsOpen = false;
        ExitRequested?.Invoke();
    }

    public bool OpenComputer()
    {
        if (!referencesValid || IsOpen)
        {
            return false;
        }

        IsOpen = true;
        computerRoot.SetActive(true);
        HidePowerMenu();
        return true;
    }

    public void ShowDesktop()
    {
        // показва desktop
        SetActive(screenOffPanel, false);
        SetActive(bootPanel, false);
        SetActive(desktopPanel, true);
        HidePowerMenu();
    }

    public void ShowOffScreen()
    {
        SetActive(screenOffPanel, true);
        SetActive(bootPanel, false);
        SetActive(desktopPanel, false);
        HidePowerMenu();
    }

    public void ShowBootScreen()
    {
        SetActive(screenOffPanel, false);
        SetActive(bootPanel, true);
        SetActive(desktopPanel, false);
        HidePowerMenu();
    }

    public void ShowShutdownScreen()
    {
        SetActive(screenOffPanel, false);
        SetActive(bootPanel, false);
        SetActive(desktopPanel, false);
        HidePowerMenu();
    }

    public void TogglePowerMenu()
    {
        if (!IsOpen || powerMenuPanel == null || desktopPanel == null || !desktopPanel.activeSelf)
        {
            return;
        }

        powerMenuPanel.SetActive(!powerMenuPanel.activeSelf);
    }

    public void HidePowerMenu()
    {
        SetActive(powerMenuPanel, false);
    }

    public void OpenMail()
    {
        if (!CanOpenDesktopApplication() || emailUIController == null)
        {
            return;
        }

        // затваря другите прозорци
        CloseAllDesktopApplications();

        // отваря приложението
        emailUIController.OpenMail();
        HidePowerMenu();
    }

    public void CloseMail()
    {
        if (emailUIController == null)
        {
            return;
        }

        // затваря приложението
        emailUIController.CloseMail();
    }

    public void OpenCaseFiles()
    {
        if (!CanOpenDesktopApplication() || caseFilesUIController == null)
        {
            return;
        }

        // затваря другите прозорци
        CloseAllDesktopApplications();

        // отваря приложението
        caseFilesUIController.OpenCaseFiles();
        HidePowerMenu();
    }

    public void CloseCaseFiles()
    {
        if (caseFilesUIController == null)
        {
            return;
        }

        // затваря приложението
        caseFilesUIController.CloseCaseFiles();
    }

    public void OpenDatabase()
    {
        OpenDesktopApplication(databaseWindow);
    }

    public void CloseDatabase()
    {
        // затваря приложението
        SetActive(databaseWindow, false);
    }

    public void OpenPhotos()
    {
        OpenDesktopApplication(photosWindow);
    }

    public void ClosePhotos()
    {
        // затваря приложението
        SetActive(photosWindow, false);
    }

    public void OpenForensics()
    {
        OpenDesktopApplication(forensicsWindow);
    }

    public void CloseForensics()
    {
        // затваря приложението
        SetActive(forensicsWindow, false);
    }

    public void CloseAllDesktopApplications()
    {
        CloseMail();
        CloseCaseFiles();
        SetActive(databaseWindow, false);
        SetActive(photosWindow, false);
        SetActive(forensicsWindow, false);
    }

    public void ExitComputer()
    {
        if (!IsOpen)
        {
            return;
        }

        CloseComputer();
        ExitRequested?.Invoke();
    }

    public void CloseComputer()
    {
        if (!IsOpen)
        {
            return;
        }

        IsOpen = false;
        HidePowerMenu();
        SetActive(computerRoot, false);
    }

    private bool CanOpenDesktopApplication()
    {
        return IsOpen && desktopPanel != null && desktopPanel.activeSelf;
    }

    private void OpenDesktopApplication(GameObject applicationWindow)
    {
        if (!CanOpenDesktopApplication() || applicationWindow == null)
        {
            return;
        }

        // затваря другите прозорци
        CloseAllDesktopApplications();

        // отваря приложението
        applicationWindow.SetActive(true);
        HidePowerMenu();
    }

    private static void SetActive(GameObject target, bool isActive)
    {
        if (target != null)
        {
            target.SetActive(isActive);
        }
    }
}
