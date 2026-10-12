using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PhoneInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private FirstPersonController playerController;
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private PhoneUIController phoneUI;

    private bool isPhoneModeActive;
    [SerializeField] private string displayName = "Телефон";

    public string DisplayName => displayName;
    public string ActionLabel => "Използвай";
    private InteractionModeController ModeController => playerInteraction != null ? playerInteraction.ModeController : null;
    public bool CanInteract => isActiveAndEnabled && HasRequiredReferences()
        && !isPhoneModeActive && phoneUI.CanOpen
        && ModeController != null && ModeController.CanEnter(this);

    private void Awake()
    {
        if (!HasRequiredReferences())
        {
            Debug.LogError("PhoneInteractable няма зададени нужните references", this);
        }
    }

    private void OnEnable()
    {
        SubscribeToExit();
    }

    private void OnDisable()
    {
        if (isPhoneModeActive)
        {
            ClosePhone();
        }

        if (phoneUI != null)
        {
            phoneUI.ExitRequested -= ClosePhone;
        }
    }

    public void Interact()
    {
        if (!CanInteract)
        {
            return;
        }

        SubscribeToExit();
        OpenPhone();
    }

    private void OpenPhone()
    {
        if (!ModeController.TryEnter(this, InteractionModeController.Mode.FocusedUI))
            return;

        if (!phoneUI.OpenPhone())
        {
            ModeController.TryExit(this);
            return;
        }

        // отваря телефона
        isPhoneModeActive = true;

    }

    private void ClosePhone()
    {
        if (!isPhoneModeActive)
        {
            return;
        }

        // затваря телефона
        isPhoneModeActive = false;
        phoneUI.ClosePhone();

        ModeController?.TryExit(this);
    }

    private bool HasRequiredReferences()
    {
        return playerController != null
            && playerInteraction != null
            && phoneUI != null;
    }

    private void SubscribeToExit()
    {
        if (phoneUI == null)
        {
            return;
        }

        phoneUI.ExitRequested -= ClosePhone;
        phoneUI.ExitRequested += ClosePhone;
    }
}
