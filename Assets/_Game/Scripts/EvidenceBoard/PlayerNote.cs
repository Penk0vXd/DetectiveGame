using System;

// PRODUCTION DIRECTION: личен текст, без доказателствена стойност или оценка.
public sealed class PlayerNote
{
    public string Id { get; }
    public string Text { get; private set; } = string.Empty;
    public float NormalizedX { get; private set; }
    public float NormalizedY { get; private set; }

    internal PlayerNote(float normalizedX, float normalizedY)
        : this(Guid.NewGuid().ToString("N"), string.Empty, normalizedX, normalizedY)
    {
    }

    private PlayerNote(string id, string text, float normalizedX, float normalizedY)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Note ID must not be empty.", nameof(id));
        ValidateCoordinate(normalizedX, nameof(normalizedX));
        ValidateCoordinate(normalizedY, nameof(normalizedY));
        Id = id;
        Text = text ?? string.Empty;
        NormalizedX = normalizedX;
        NormalizedY = normalizedY;
    }

    internal static PlayerNote Restore(string id, string text, float normalizedX, float normalizedY)
    {
        return new PlayerNote(id, text, normalizedX, normalizedY);
    }

    public void SetText(string text) => Text = text ?? string.Empty;

    public void SetPosition(float normalizedX, float normalizedY)
    {
        ValidateCoordinate(normalizedX, nameof(normalizedX));
        ValidateCoordinate(normalizedY, nameof(normalizedY));
        NormalizedX = normalizedX;
        NormalizedY = normalizedY;
    }

    private static void ValidateCoordinate(float value, string name)
    {
        if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f || value > 1f)
            throw new ArgumentOutOfRangeException(name);
    }
}
