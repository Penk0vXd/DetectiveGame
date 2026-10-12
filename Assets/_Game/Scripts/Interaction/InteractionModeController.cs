using UnityEngine;

// PROTOTYPE CODE: един собственик на управлението между office инструментите.
public sealed class InteractionModeController
{
    public enum Mode
    {
        World,
        FocusedUI,
        Inspection
    }

    private readonly FirstPersonController movement;
    private readonly PlayerInteraction interaction;
    private MonoBehaviour activeOwner;
    private float timeScaleBeforePause = 1f;

    public Mode CurrentMode { get; private set; } = Mode.World;
    public MonoBehaviour ActiveOwner => activeOwner;
    public bool IsPaused { get; private set; }

    public InteractionModeController(FirstPersonController movement, PlayerInteraction interaction)
    {
        this.movement = movement;
        this.interaction = interaction;
    }

    public bool CanEnter(MonoBehaviour owner)
    {
        return !IsPaused && owner != null && owner.isActiveAndEnabled
            && CurrentMode == Mode.World && activeOwner == null
            && movement != null && interaction != null;
    }

    public bool TryEnter(MonoBehaviour owner, Mode nextMode)
    {
        if (!CanEnter(owner) || nextMode == Mode.World)
            return false;

        activeOwner = owner;
        CurrentMode = nextMode;
        ApplyControls();
        return true;
    }

    public bool TryExit(MonoBehaviour owner)
    {
        // Закъснял exit от стар инструмент не освобождава новия owner.
        if (!ReferenceEquals(activeOwner, owner) || CurrentMode == Mode.World)
            return false;

        activeOwner = null;
        CurrentMode = Mode.World;
        ApplyControls();
        return true;
    }

    public bool TryPause()
    {
        if (IsPaused || movement == null || interaction == null)
            return false;

        // Pause е overlay: активният инструмент и екранът му остават отворени.
        timeScaleBeforePause = Time.timeScale;
        IsPaused = true;
        Time.timeScale = 0f;
        ApplyControls();
        return true;
    }

    public bool TryResume()
    {
        if (!IsPaused)
            return false;

        IsPaused = false;
        Time.timeScale = timeScaleBeforePause;
        ApplyControls();
        return true;
    }

    public void RestoreTimeOnShutdown()
    {
        // Не оставя замразено време при disable/scene unload.
        if (IsPaused)
            TryResume();
    }

    private void ApplyControls()
    {
        if (movement == null || interaction == null)
            return;

        movement.SetControlEnabled(!IsPaused && CurrentMode == Mode.World);
        // При inspection E остава активен само за връщане на документа.
        interaction.SetInteractionEnabled(!IsPaused && CurrentMode != Mode.FocusedUI);
        bool showCursor = IsPaused || CurrentMode == Mode.FocusedUI;
        Cursor.lockState = showCursor ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = showCursor;
    }
}
