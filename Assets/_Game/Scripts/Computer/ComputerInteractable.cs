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
        if (!referencesValid || isComputerModeActive || computerUI.IsOpen)
        {
            return;
        }

        OpenComputer();
    }

    private void OpenComputer()
    {
        if (!computerUI.OpenComputer())
        {
            return;
        }

        // отваря компютъра
        isComputerModeActive = true;

        // спира управлението
        playerController.SetControlEnabled(false);
        playerInteraction.SetInteractionEnabled(false);

        // показва курсора
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

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

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        playerController.SetControlEnabled(true);
        playerInteraction.SetInteractionEnabled(true);
    }
}
