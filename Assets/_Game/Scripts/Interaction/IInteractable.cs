public interface IInteractable
{
    // Данни за usability; не описват важността на обекта за разследването.
    string DisplayName { get; }
    string ActionLabel { get; }
    bool CanInteract { get; }
    void Interact();
}
