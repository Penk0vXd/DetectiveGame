using System;

// PRODUCTION DIRECTION: личен текст, без доказателствена стойност или оценка.
public sealed class PlayerNote
{
    public string Id { get; }
    public string Text { get; private set; } = string.Empty;
    public float NormalizedX { get; }
    public float NormalizedY { get; }

    internal PlayerNote(float normalizedX, float normalizedY)
    {
        ValidateCoordinate(normalizedX, nameof(normalizedX));
        ValidateCoordinate(normalizedY, nameof(normalizedY));
        Id = Guid.NewGuid().ToString("N");
        NormalizedX = normalizedX;
        NormalizedY = normalizedY;
    }

    public void SetText(string text) => Text = text ?? string.Empty;

    private static void ValidateCoordinate(float value, string name)
    {
        if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f || value > 1f)
            throw new ArgumentOutOfRangeException(name);
    }
}
