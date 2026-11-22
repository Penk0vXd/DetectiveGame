using System;

public enum AttachedSourceType
{
    Document = 0,
    Photo = 1
}

// PRODUCTION DIRECTION: копие на съществуващ източник (документ/снимка), напълно отделно от PlayerNote.
public sealed class AttachedSource
{
    public string AttachmentId { get; }
    public string SourceId { get; }
    public AttachedSourceType SourceType { get; }
    public float NormalizedX { get; private set; }
    public float NormalizedY { get; private set; }

    internal AttachedSource(string sourceId, AttachedSourceType sourceType, float normalizedX, float normalizedY)
        : this(Guid.NewGuid().ToString("N"), sourceId, sourceType, normalizedX, normalizedY)
    {
    }

    private AttachedSource(string attachmentId, string sourceId, AttachedSourceType sourceType,
        float normalizedX, float normalizedY)
    {
        if (string.IsNullOrWhiteSpace(attachmentId))
            throw new ArgumentException("Attachment ID must not be empty.", nameof(attachmentId));
        if (string.IsNullOrWhiteSpace(sourceId))
            throw new ArgumentException("Source ID must not be empty.", nameof(sourceId));
        if (!IsValidSourceType(sourceType))
            throw new ArgumentOutOfRangeException(nameof(sourceType));
        ValidateCoordinate(normalizedX, nameof(normalizedX));
        ValidateCoordinate(normalizedY, nameof(normalizedY));

        AttachmentId = attachmentId;
        SourceId = sourceId;
        SourceType = sourceType;
        NormalizedX = normalizedX;
        NormalizedY = normalizedY;
    }

    internal static AttachedSource Restore(string attachmentId, string sourceId, AttachedSourceType sourceType,
        float normalizedX, float normalizedY)
    {
        return new AttachedSource(attachmentId, sourceId, sourceType, normalizedX, normalizedY);
    }

    public void SetPosition(float normalizedX, float normalizedY)
    {
        ValidateCoordinate(normalizedX, nameof(normalizedX));
        ValidateCoordinate(normalizedY, nameof(normalizedY));
        NormalizedX = normalizedX;
        NormalizedY = normalizedY;
    }

    internal static bool IsValidSourceType(AttachedSourceType sourceType)
    {
        return sourceType == AttachedSourceType.Document || sourceType == AttachedSourceType.Photo;
    }

    private static void ValidateCoordinate(float value, string name)
    {
        if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f || value > 1f)
            throw new ArgumentOutOfRangeException(name);
    }
}
