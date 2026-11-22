using System;
using System.Collections.Generic;

// PRODUCTION DIRECTION: отделен session state; persistence минава през OfficeSaveController DTO.
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

    public bool Remove(string noteId)
    {
        if (string.IsNullOrEmpty(noteId)) return false;
        int index = notes.FindIndex(note => note.Id == noteId);
        if (index < 0) return false;
        notes.RemoveAt(index);
        return true;
    }

    public void ReplaceAll(IEnumerable<PlayerNote> restoredNotes)
    {
        if (restoredNotes == null) throw new ArgumentNullException(nameof(restoredNotes));

        var next = new List<PlayerNote>();
        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (PlayerNote note in restoredNotes)
        {
            if (note == null) throw new ArgumentException("Restored notes cannot contain null.", nameof(restoredNotes));
            if (!seenIds.Add(note.Id)) throw new ArgumentException("Duplicate note ID: " + note.Id, nameof(restoredNotes));
            next.Add(note);
        }

        notes.Clear();
        notes.AddRange(next);
    }
}
