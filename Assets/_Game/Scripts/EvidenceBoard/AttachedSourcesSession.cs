using System;
using System.Collections.Generic;

// PRODUCTION DIRECTION: отделна колекция за ръчно прикачени копия; не споделя ID-та или логика с PlayerNotes.
public sealed class AttachedSourcesSession
{
    private readonly List<AttachedSource> sources = new List<AttachedSource>();
    public IReadOnlyList<AttachedSource> Sources { get; }

    public AttachedSourcesSession() => Sources = sources.AsReadOnly();

    public bool ContainsSource(string sourceId, AttachedSourceType sourceType)
    {
        if (string.IsNullOrWhiteSpace(sourceId)) return false;
        for (int i = 0; i < sources.Count; i++)
        {
            if (sources[i].SourceType == sourceType
                && string.Equals(sources[i].SourceId, sourceId, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    public bool TryAttach(string sourceId, AttachedSourceType sourceType, float normalizedX, float normalizedY,
        out AttachedSource attached)
    {
        attached = default!;
        if (string.IsNullOrWhiteSpace(sourceId) || !AttachedSource.IsValidSourceType(sourceType))
            return false;
        if (ContainsSource(sourceId, sourceType))
            return false;

        attached = new AttachedSource(sourceId, sourceType, normalizedX, normalizedY);
        sources.Add(attached);
        return true;
    }

    public bool Remove(string attachmentId)
    {
        if (string.IsNullOrWhiteSpace(attachmentId)) return false;
        int index = sources.FindIndex(item => string.Equals(item.AttachmentId, attachmentId, StringComparison.Ordinal));
        if (index < 0) return false;
        sources.RemoveAt(index);
        return true;
    }

    public void ReplaceAll(IEnumerable<AttachedSource> restoredSources)
    {
        if (restoredSources == null) throw new ArgumentNullException(nameof(restoredSources));

        var next = new List<AttachedSource>();
        var seenAttachmentIds = new HashSet<string>(StringComparer.Ordinal);
        var seenSourceKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (AttachedSource item in restoredSources)
        {
            if (item == null)
                throw new ArgumentException("Restored attached sources cannot contain null.", nameof(restoredSources));
            if (!seenAttachmentIds.Add(item.AttachmentId))
                throw new ArgumentException("Duplicate attachment ID: " + item.AttachmentId, nameof(restoredSources));
            string sourceKey = ((int)item.SourceType) + ":" + item.SourceId;
            if (!seenSourceKeys.Add(sourceKey))
                throw new ArgumentException("Duplicate attached source: " + sourceKey, nameof(restoredSources));
            next.Add(item);
        }

        sources.Clear();
        sources.AddRange(next);
    }
}
