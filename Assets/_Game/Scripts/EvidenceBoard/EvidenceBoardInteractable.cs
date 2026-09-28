using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EvidenceBoardInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private FirstPersonController playerController;
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private EvidenceBoardUIController boardUI;

    private bool isBoardModeActive;

    private void Awake()
    {
        if (!HasRequiredReferences())
        {
            Debug.LogError("EvidenceBoardInteractable няма зададени нужните references", this);
        }
    }

    private void OnEnable()
    {
        SubscribeToExit();
    }

    private void OnDisable()
    {
        if (isBoardModeActive)
        {
            CloseBoard();
        }

        if (boardUI != null)
        {
            boardUI.ExitRequested -= CloseBoard;
        }
    }

    public void Interact()
    {
        if (!HasRequiredReferences() || isBoardModeActive || boardUI.IsOpen)
        {
            return;
        }

        SubscribeToExit();
        OpenBoard();
    }

    private void OpenBoard()
    {
        if (!boardUI.ShowBoard())
        {
            return;
        }

        // отваря таблото
        isBoardModeActive = true;

        // спира управлението
        playerController.SetControlEnabled(false);
        playerInteraction.SetInteractionEnabled(false);

        // показва курсора
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void CloseBoard()
    {
        if (!isBoardModeActive)
        {
            return;
        }

        // затваря таблото
        isBoardModeActive = false;

        if (boardUI != null)
        {
            boardUI.CloseBoard();
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // връща управлението
        playerController.SetControlEnabled(true);
        playerInteraction.SetInteractionEnabled(true);
    }

    private bool HasRequiredReferences()
    {
        return playerController != null
            && playerInteraction != null
            && boardUI != null;
    }

    private void SubscribeToExit()
    {
        if (boardUI == null)
        {
            return;
        }

        boardUI.ExitRequested -= CloseBoard;
        boardUI.ExitRequested += CloseBoard;
    }
}
