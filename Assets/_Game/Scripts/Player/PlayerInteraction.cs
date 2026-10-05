using UnityEngine;

[RequireComponent(typeof(PlayerInputReader))]
public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private float interactionDistance = 3f;

    private Camera playerCamera;
    private PlayerInputReader inputReader;
    private DocumentInteractable inspectedDocument;
    private bool interactionEnabled = true;

    private void Awake()
    {
        inputReader = GetComponent<PlayerInputReader>();

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
        if (!interactionEnabled)
        {
            return;
        }

        if (!inputReader.ReadInteract())
        {
            return;
        }

        if (inspectedDocument != null)
        {
            if (inspectedDocument.IsInspecting)
            {
                inspectedDocument.Interact();
            }

            inspectedDocument = null;
            return;
        }

        // проверява за обект
        if (Physics.Raycast(
            playerCamera.transform.position,
            playerCamera.transform.forward,
            out RaycastHit hit,
            interactionDistance))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable != null)
            {
                interactable.Interact();

                if (interactable is DocumentInteractable document && document.IsInspecting)
                {
                    inspectedDocument = document;
                }
            }
        }
    }

    public void SetInteractionEnabled(bool isEnabled)
    {
        // включва или спира взаимодействието
        interactionEnabled = isEnabled;
    }
}
