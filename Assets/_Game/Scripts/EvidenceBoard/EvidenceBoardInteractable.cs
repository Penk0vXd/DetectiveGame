using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EvidenceBoardInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private FirstPersonController playerController;
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private EvidenceBoardUIController boardUI;

    private bool isBoardModeActive;
    [SerializeField] private string displayName = "Табло";

    public string DisplayName => displayName;
    public string ActionLabel => "Разгледай";
    private InteractionModeController ModeController => playerInteraction != null ? playerInteraction.ModeController : null;
    public bool CanInteract => isActiveAndEnabled && HasRequiredReferences()
        && !isBoardModeActive && boardUI.CanOpen
        && ModeController != null && ModeController.CanEnter(this);

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
        if (!CanInteract)
        {
            return;
        }

        SubscribeToExit();
        OpenBoard();
    }

    private void OpenBoard()
    {
        if (!ModeController.TryEnter(this, InteractionModeController.Mode.FocusedUI))
            return;

        if (!boardUI.ShowBoard())
        {
            ModeController.TryExit(this);
            return;
        }

        // отваря таблото
        isBoardModeActive = true;

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

        ModeController?.TryExit(this);
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
