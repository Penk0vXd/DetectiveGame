using System;
using System.Collections.Generic;

// PROTOTYPE CODE: временни връзки и бележки за една Board сесия, без save identity.
public sealed class EvidenceBoardSession
{
    private readonly int cardCount;
    private readonly HashSet<(int First, int Second)> links = new HashSet<(int, int)>();

    public IReadOnlyCollection<(int First, int Second)> Links => links;
    public string Notes { get; set; } = string.Empty;

    public EvidenceBoardSession(int cardCount)
    {
        if (cardCount < 2) throw new ArgumentOutOfRangeException(nameof(cardCount));
        this.cardCount = cardCount;
    }

    public bool TryLink(int first, int second)
    {
        if (!ValidPair(first, second)) return false;
        return links.Add(Normalize(first, second));
    }

    public bool HasLink(int first, int second)
    {
        return ValidPair(first, second) && links.Contains(Normalize(first, second));
    }

    public bool RemoveLink(int first, int second)
    {
        return ValidPair(first, second) && links.Remove(Normalize(first, second));
    }

    private bool ValidPair(int first, int second)
    {
        return first >= 0 && first < cardCount && second >= 0 && second < cardCount && first != second;
    }

    private static (int First, int Second) Normalize(int first, int second)
    {
        return first < second ? (first, second) : (second, first);
    }
}
