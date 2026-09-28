using UnityEngine;

public class TestInteractable : MonoBehaviour, IInteractable
{
    public void Interact()
    {
        Debug.Log("Interacted");

        transform.Rotate(0f, 45f, 0f);
    }
}