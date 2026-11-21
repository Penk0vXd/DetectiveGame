using System.Collections.Generic;

// PRODUCTION DIRECTION: отделен session state; EB-001 не включва disk persistence.
public sealed class PlayerNotesSession
{
    private readonly List<PlayerNote> notes = new List<PlayerNote>();
    public IReadOnlyList<PlayerNote> Notes { get; }

    public PlayerNotesSession() => Notes = notes.AsReadOnly();

    public PlayerNote Create(float normalizedX, float normalizedY)
    {
        var note = new PlayerNote(normalizedX, normalizedY);
        notes.Add(note);
        return note;
    }
}
