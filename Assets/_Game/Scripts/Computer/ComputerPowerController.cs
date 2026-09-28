using System;
using System.Collections;
using UnityEngine;

public class ComputerPowerController : MonoBehaviour
{
    [SerializeField] private ComputerUIController computerUI;
    [SerializeField] private float bootDuration = 3f;
    [SerializeField] private float shutdownDuration = 1.5f;

    private Coroutine powerRoutine;
    private bool isShuttingDown;

    public ComputerPowerState CurrentState { get; private set; } = ComputerPowerState.Off;

    public event Action ComputerModeExitRequested;

    private void Awake()
    {
        if (computerUI == null)
        {
            Debug.LogError("ComputerPowerController няма зададен ComputerUIController", this);
            return;
        }

        SetState(ComputerPowerState.Off);
    }

    public void EnterComputerMode()
    {
        if (computerUI == null)
        {
            return;
        }

        switch (CurrentState)
        {
            case ComputerPowerState.Off:
                StartBoot();
                break;
            case ComputerPowerState.Booting:
                computerUI.ShowBootScreen();
                break;
            case ComputerPowerState.On:
                if (isShuttingDown)
                {
                    computerUI.ShowShutdownScreen();
                    break;
                }

                computerUI.ShowDesktop();
                break;
            case ComputerPowerState.Sleeping:
                Wake();
                break;
        }
    }

    public void Sleep()
    {
        if (!CanStartPowerAction())
        {
            return;
        }

        // приспива компютъра
        SetState(ComputerPowerState.Sleeping);
        ComputerModeExitRequested?.Invoke();
    }

    public void Restart()
    {
        if (!CanStartPowerAction())
        {
            return;
        }

        // рестартира компютъра
        computerUI.CloseAllDesktopApplications();
        StartBoot();
    }

    public void ShutDown()
    {
        if (!CanStartPowerAction())
        {
            return;
        }

        // изключва компютъра
        computerUI.CloseAllDesktopApplications();
        isShuttingDown = true;
        computerUI.ShowShutdownScreen();
        powerRoutine = StartCoroutine(ShutdownRoutine());
    }

    private bool CanStartPowerAction()
    {
        return computerUI != null
            && CurrentState == ComputerPowerState.On
            && powerRoutine == null;
    }

    private void StartBoot()
    {
        if (powerRoutine != null || CurrentState == ComputerPowerState.Booting)
        {
            return;
        }

        // започва boot
        SetState(ComputerPowerState.Booting);
        powerRoutine = StartCoroutine(BootRoutine());
    }

    private IEnumerator BootRoutine()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, bootDuration));

        powerRoutine = null;
        SetState(ComputerPowerState.On);
    }

    private IEnumerator ShutdownRoutine()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, shutdownDuration));

        powerRoutine = null;
        isShuttingDown = false;
        SetState(ComputerPowerState.Off);
        ComputerModeExitRequested?.Invoke();
    }

    private void Wake()
    {
        // включва компютъра
        SetState(ComputerPowerState.On);
    }

    private void SetState(ComputerPowerState state)
    {
        CurrentState = state;

        switch (state)
        {
            case ComputerPowerState.Off:
            case ComputerPowerState.Sleeping:
                computerUI.ShowOffScreen();
                break;
            case ComputerPowerState.Booting:
                computerUI.ShowBootScreen();
                break;
            case ComputerPowerState.On:
                computerUI.ShowDesktop();
                break;
        }
    }
}
