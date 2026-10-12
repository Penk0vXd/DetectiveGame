using UnityEngine;

[RequireComponent(typeof(PlayerInputReader))]
public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private float interactionDistance = 3f;

    private Camera playerCamera;
    private PlayerInputReader inputReader;
    private DocumentInteractable inspectedDocument;
    private bool interactionEnabled = true;

    public InteractionModeController ModeController { get; private set; }

    // PROTOTYPE CODE: общ target за действието и interaction HUD.
    private MonoBehaviour currentTarget;

    public bool IsWorldInteractionActive => isActiveAndEnabled && interactionEnabled
        && playerCamera != null && inputReader != null && inputReader.isActiveAndEnabled
        && (inspectedDocument == null || !inspectedDocument.IsInspecting);

    public IInteractable CurrentTarget
    {
        get
        {
            if (!IsWorldInteractionActive || currentTarget == null || !currentTarget.isActiveAndEnabled)
                return null;

            IInteractable target = currentTarget as IInteractable;
            return target != null && target.CanInteract ? target : null;
        }
    }

    private void Awake()
    {
        inputReader = GetComponent<PlayerInputReader>();
        ModeController = new InteractionModeController(GetComponent<FirstPersonController>(), this);

        // взима камерата
        playerCamera = GetComponentInChildren<Camera>();

        if (playerCamera == null)
        {
            Debug.LogError("PlayerInteraction не намери Camera в Player", this);
            enabled = false;
        }
    }

    private void Update()
    {
        // Старият target не трябва да остава при загуба на focus или управление.
        currentTarget = null;

        // Disabled document вече е върнат от OnDisable и не бива да задържа inspection.
        if (inspectedDocument != null && (!inspectedDocument.isActiveAndEnabled || !inspectedDocument.IsInspecting))
            inspectedDocument = null;

        if (!interactionEnabled || inputReader == null || !inputReader.isActiveAndEnabled)
        {
            return;
        }

        // Esc има приоритет пред E и не задейства скрития инструмент.
        if (inputReader.ReadPause())
            return;

        if (inspectedDocument != null)
        {
            // Документът се затваря с E дори когато raycast не сочи към него.
            if (inputReader.ReadInteract())
            {
                if (inspectedDocument.IsInspecting)
                {
                    inspectedDocument.Interact();
                }

                inspectedDocument = null;
            }

            return;
        }

        UpdateTarget();

        if (!inputReader.ReadInteract())
        {
            return;
        }

        // Използва същия target, който ще прочете HUD, без втори raycast.
        IInteractable interactable = CurrentTarget;
        if (interactable == null)
        {
            return;
        }

        interactable.Interact();

        if (interactable is DocumentInteractable document && document.IsInspecting)
        {
            inspectedDocument = document;
            currentTarget = null;
        }
    }

    private void UpdateTarget()
    {
        // Проверява преди E, за да може HUD да показва наличното действие.
        if (Physics.Raycast(
            playerCamera.transform.position,
            playerCamera.transform.forward,
            out RaycastHit hit,
            interactionDistance))
        {
            MonoBehaviour candidate = hit.collider.GetComponent<IInteractable>() as MonoBehaviour;
            if (candidate != null && candidate.isActiveAndEnabled)
            {
                currentTarget = candidate;
            }
        }
    }

    public void SetInteractionEnabled(bool isEnabled)
    {
        // включва или спира взаимодействието
        interactionEnabled = isEnabled;
        if (!isEnabled)
        {
            currentTarget = null;
        }
    }

    private void OnDisable()
    {
        ModeController?.RestoreTimeOnShutdown();
        currentTarget = null;
    }
}
