using UnityEngine;

public class PlayerInputReader : MonoBehaviour
{
    private DetectiveGameInput input;

    private void Awake()
    {
        // създава input actions
        CreateInput();
    }

    private void OnEnable()
    {
        // включва player input
        CreateInput();
        input.Player.Enable();
    }

    private void OnDisable()
    {
        if (input == null)
        {
            return;
        }

        // изключва player input
        input.Player.Disable();
    }

    private void OnDestroy()
    {
        if (input == null)
        {
            return;
        }

        input.Dispose();
        input = null;
    }

    public Vector2 ReadMove()
    {
        EnsureInputReady();

        // чете движението
        return input.Player.Move.ReadValue<Vector2>();
    }

    public Vector2 ReadLook()
    {
        EnsureInputReady();

        // чете мишката
        return input.Player.Look.ReadValue<Vector2>();
    }

    public bool ReadInteract()
    {
        EnsureInputReady();
        return input.Player.Interact.WasPressedThisFrame();
    }

    private void CreateInput()
    {
        if (input != null)
        {
            return;
        }

        input = new DetectiveGameInput();
    }

    private void EnsureInputReady()
    {
        if (input != null)
        {
            return;
        }

        // възстановява input след reload
        CreateInput();
        input.Player.Enable();
    }
}
