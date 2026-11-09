using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputReader : MonoBehaviour
{
    private DetectiveGameInput input;

    private void Awake()
    {
        // създава input actions
        CreateInput();
        OfficeSettingsStore.ApplyBindings(this);
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

    public bool ReadPause()
    {
        EnsureInputReady();
        return input.Player.Pause.WasPressedThisFrame();
    }

    public string GetInteractBindingLabel()
    {
        EnsureInputReady();
        // Чете реалния binding, включително бъдещ runtime override.
        return input.Player.Interact.GetBindingDisplayString();
    }

    public string GetBindingLabel(int index)
    {
        EnsureInputReady();
        return index < 4 ? input.Player.Move.GetBindingDisplayString(index + 1)
            : input.Player.Interact.GetBindingDisplayString(0);
    }

    public void ApplyBindings(string[] paths)
    {
        EnsureInputReady();
        for (int i = 0; i < 4; i++)
            input.Player.Move.ApplyBindingOverride(i + 1, paths[i]);
        input.Player.Interact.ApplyBindingOverride(0, paths[4]);
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
