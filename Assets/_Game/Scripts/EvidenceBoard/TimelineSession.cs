using System;
using System.Collections.Generic;

// Отделна колекция за ръчната времева линия; не споделя ID-та или логика с PlayerNotes и AttachedSources.
public sealed class TimelineSession
{
    private readonly List<TimelineEvent> events = new List<TimelineEvent>();
    private int nextInsertionOrder;

    public IReadOnlyList<TimelineEvent> Events { get; }

    public TimelineSession() => Events = events.AsReadOnly();

    public bool TryGetById(string eventId, out TimelineEvent found)
    {
        found = default!;
        if (string.IsNullOrWhiteSpace(eventId)) return false;
        for (int i = 0; i < events.Count; i++)
        {
            if (string.Equals(events[i].Id, eventId, StringComparison.Ordinal))
            {
                found = events[i];
                return true;
            }
        }
        return false;
    }

    public bool TryCreate(string date, string optionalTime, string title, string description,
        out TimelineEvent created, out string errorMessage)
    {
        created = default!;
        if (!TimelineEvent.TryValidateFields(date, optionalTime, title,
                out string normalizedDate, out int dateSortKey,
                out string normalizedTime, out int timeMinutes,
                out string normalizedTitle, out errorMessage))
        {
            return false;
        }

        created = TimelineEvent.CreateNew(
            normalizedDate,
            dateSortKey,
            normalizedTime,
            timeMinutes,
            normalizedTitle,
            description ?? string.Empty,
            nextInsertionOrder++);
        events.Add(created);
        events.Sort(TimelineEvent.CompareChronological);
        return true;
    }

    public bool TryUpdate(string eventId, string date, string optionalTime, string title, string description,
        out string errorMessage)
    {
        if (!TryGetById(eventId, out TimelineEvent target))
        {
            errorMessage = "Събитието не беше намерено.";
            return false;
        }

        if (!TimelineEvent.TryValidateFields(date, optionalTime, title,
                out string normalizedDate, out int dateSortKey,
                out string normalizedTime, out int timeMinutes,
                out string normalizedTitle, out errorMessage))
        {
            return false;
        }

        target.ApplyValidatedUpdate(normalizedDate, dateSortKey, normalizedTime, timeMinutes, normalizedTitle, description);
        events.Sort(TimelineEvent.CompareChronological);
        return true;
    }

    public bool Remove(string eventId)
    {
        if (string.IsNullOrWhiteSpace(eventId)) return false;
        int index = events.FindIndex(item => string.Equals(item.Id, eventId, StringComparison.Ordinal));
        if (index < 0) return false;
        events.RemoveAt(index);
        return true;
    }

    public void ReplaceAll(IEnumerable<TimelineEvent> restoredEvents)
    {
        if (restoredEvents == null) throw new ArgumentNullException(nameof(restoredEvents));

        var next = new List<TimelineEvent>();
        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        int maxOrder = -1;

        foreach (TimelineEvent item in restoredEvents)
        {
            if (item == null)
                throw new ArgumentException("Restored timeline events cannot contain null.", nameof(restoredEvents));
            if (!seenIds.Add(item.Id))
                throw new ArgumentException("Duplicate timeline event ID: " + item.Id, nameof(restoredEvents));
            if (item.InsertionOrder > maxOrder)
                maxOrder = item.InsertionOrder;
            next.Add(item);
        }

        next.Sort(TimelineEvent.CompareChronological);
        events.Clear();
        events.AddRange(next);
        nextInsertionOrder = maxOrder + 1;
    }
}
