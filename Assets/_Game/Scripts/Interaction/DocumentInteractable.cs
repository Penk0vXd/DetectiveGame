using UnityEngine;

public class DocumentInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private FirstPersonController playerController;
    [SerializeField] private float inspectionDistance = 0.6f;
    [SerializeField] private float verticalOffset;
    [SerializeField] private Vector3 inspectionRotation;

    private Transform originalParent;
    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;

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

        // пази старите стойности
        originalParent = transform.parent;
        originalLocalPosition = transform.localPosition;
        originalLocalRotation = transform.localRotation;

        // мести документа пред камерата
        transform.SetParent(playerCamera.transform, false);
        transform.localPosition = new Vector3(0f, verticalOffset, inspectionDistance);
        transform.localRotation = Quaternion.Euler(inspectionRotation);

        // спира управлението
        playerController.SetControlEnabled(false);
        IsInspecting = true;
    }

    private void CloseInspection()
    {
        // връща документа
        transform.SetParent(originalParent, false);
        transform.localPosition = originalLocalPosition;
        transform.localRotation = originalLocalRotation;

        // включва управлението
        if (playerController != null)
        {
            playerController.SetControlEnabled(true);
        }

        IsInspecting = false;
    }
}
