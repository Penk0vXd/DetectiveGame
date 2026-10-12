using UnityEngine;

public class TestInteractable : MonoBehaviour, IInteractable
{
    public string DisplayName => "Тестов обект";
    public string ActionLabel => "Завърти";
    public bool CanInteract => isActiveAndEnabled;

    public void Interact()
    {
        Debug.Log("Interacted");

        transform.Rotate(0f, 45f, 0f);
    }
}
