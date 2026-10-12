using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ComputerInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private FirstPersonController playerController;
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private ComputerUIController computerUI;
    [SerializeField] private ComputerPowerController powerController;

    private bool referencesValid;
    private bool isComputerModeActive;
    [SerializeField] private string displayName = "Компютър";

    public string DisplayName => displayName;
    public string ActionLabel => "Използвай";
    private InteractionModeController ModeController => playerInteraction != null ? playerInteraction.ModeController : null;
    public bool CanInteract => isActiveAndEnabled && playerController != null
        && playerInteraction != null && computerUI != null && computerUI.CanOpen
        && powerController != null && powerController.isActiveAndEnabled && !isComputerModeActive
        && ModeController != null && ModeController.CanEnter(this);

    private void Awake()
    {
        referencesValid = playerController != null
            && playerInteraction != null
            && computerUI != null
            && powerController != null;

        if (!referencesValid)
        {
            Debug.LogError("ComputerInteractable няма зададени нужните references", this);
        }
    }

    private void OnEnable()
    {
        if (computerUI == null)
        {
            return;
        }

        computerUI.ExitRequested -= CloseComputer;
        computerUI.ExitRequested += CloseComputer;

        if (powerController != null)
        {
            powerController.ComputerModeExitRequested -= CloseComputer;
            powerController.ComputerModeExitRequested += CloseComputer;
        }
    }

    private void OnDisable()
    {
        if (isComputerModeActive)
        {
            CloseComputer();
        }

        if (computerUI != null)
        {
            computerUI.ExitRequested -= CloseComputer;
        }

        if (powerController != null)
        {
            powerController.ComputerModeExitRequested -= CloseComputer;
        }
    }

    public void Interact()
    {
        if (!CanInteract)
        {
            return;
        }

        OpenComputer();
    }

    private void OpenComputer()
    {
        if (!ModeController.TryEnter(this, InteractionModeController.Mode.FocusedUI))
            return;

        if (!computerUI.OpenComputer())
        {
            ModeController.TryExit(this);
            return;
        }

        // отваря компютъра
        isComputerModeActive = true;

        powerController.EnterComputerMode();
    }

    private void CloseComputer()
    {
        if (!isComputerModeActive)
        {
            return;
        }

        // затваря компютъра
        isComputerModeActive = false;
        computerUI.CloseComputer();

        ModeController?.TryExit(this);
    }
}
