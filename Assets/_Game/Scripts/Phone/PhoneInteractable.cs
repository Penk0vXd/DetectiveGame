using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PhoneInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private FirstPersonController playerController;
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private PhoneUIController phoneUI;

    private bool isPhoneModeActive;

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
        if (!HasRequiredReferences() || isPhoneModeActive || phoneUI.IsOpen)
        {
            return;
        }

        SubscribeToExit();
        OpenPhone();
    }

    private void OpenPhone()
    {
        if (!phoneUI.OpenPhone())
        {
            return;
        }

        // отваря телефона
        isPhoneModeActive = true;

        // спира управлението
        playerController.SetControlEnabled(false);
        playerInteraction.SetInteractionEnabled(false);

        // показва курсора
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
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
