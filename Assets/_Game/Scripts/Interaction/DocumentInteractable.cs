using UnityEngine;

public class DocumentInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private FirstPersonController playerController;
    [SerializeField] private float inspectionDistance = 0.6f;
    [SerializeField] private float verticalOffset;
    [SerializeField] private Vector3 inspectionRotation;
    [SerializeField] private string displayName = "Документ";

    public string DisplayName => displayName;
    public string ActionLabel => "Разгледай";
    private InteractionModeController ModeController => playerController != null
        ? playerController.GetComponent<PlayerInteraction>()?.ModeController : null;
    public bool CanInteract => isActiveAndEnabled && playerCamera != null && playerController != null
        && ModeController != null && ModeController.CanEnter(this);

    private Transform originalParent;
    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;
    private Vector3 originalLocalScale;
    private Vector3 originalWorldPosition;
    private Quaternion originalWorldRotation;

    public bool IsInspecting { get; private set; }

    public void Interact()
    {
        if (IsInspecting)
        {
            CloseInspection();
            return;
        }

        OpenInspection();
    }

    private void OpenInspection()
    {
        if (playerCamera == null || playerController == null)
        {
            Debug.LogError("DocumentInteractable няма зададени Player Camera и Player Controller", this);
            return;
        }

        if (ModeController == null || !ModeController.TryEnter(this, InteractionModeController.Mode.Inspection))
            return;

        // пази старите стойности
        originalParent = transform.parent;
        originalLocalPosition = transform.localPosition;
        originalLocalRotation = transform.localRotation;
        originalLocalScale = transform.localScale;
        originalWorldPosition = transform.position;
        originalWorldRotation = transform.rotation;

        // мести документа пред камерата
        transform.SetParent(playerCamera.transform, false);
        transform.localPosition = new Vector3(0f, verticalOffset, inspectionDistance);
        transform.localRotation = Quaternion.Euler(inspectionRotation);

        IsInspecting = true;
    }

    private void CloseInspection()
    {
        // връща документа
        if (originalParent != null)
        {
            transform.SetParent(originalParent, false);
            transform.localPosition = originalLocalPosition;
            transform.localRotation = originalLocalRotation;
        }
        else
        {
            transform.SetParent(null, true);
            transform.position = originalWorldPosition;
            transform.rotation = originalWorldRotation;
        }
        transform.localScale = originalLocalScale;

        IsInspecting = false;
        ModeController?.TryExit(this);
    }

    private void OnDisable()
    {
        if (IsInspecting)
            CloseInspection();
    }
}
