using System;
using System.Collections.Generic;

public enum ConversationChannel { Chat, Phone }
public enum ConversationTurnStatus { Pending, Accepted, Failed, Cancelled }

public sealed class ConversationTurn
{
    public string SpeakerId { get; }
    public string Text { get; }
    public ConversationTurnStatus Status { get; internal set; }
    public string Error { get; internal set; }
    public ConversationTurn(string speakerId, string text, ConversationTurnStatus status)
    { SpeakerId = speakerId; Text = text; Status = status; }
}

public sealed class ConversationState
{
    private readonly List<ConversationTurn> turns = new List<ConversationTurn>();
    public string PersonId { get; }
    public ConversationChannel Channel { get; }
    public IReadOnlyList<ConversationTurn> Transcript => turns.AsReadOnly();
    // Няма одобрено действие за представяне на материали в този прототип.
    public IReadOnlyList<string> VerifiedMaterialIds => Array.Empty<string>();
    public ConversationState(string personId, ConversationChannel channel)
    { PersonId = personId; Channel = channel; }
    internal void Add(ConversationTurn turn) => turns.Add(turn);
}

public sealed class ConversationRequest
{
    public string PersonId { get; }
    public ConversationChannel Channel { get; }
    public string Text { get; }
    public IReadOnlyList<string> MaterialIds { get; }
    public ConversationRequest(string personId, ConversationChannel channel, string text, params string[] materialIds)
    {
        PersonId = personId; Channel = channel; Text = text;
        MaterialIds = Array.AsReadOnly((string[])(materialIds ?? Array.Empty<string>()).Clone());
    }
}

public sealed class ConversationReply
{
    public string PersonId { get; }
    public string Text { get; }
    public string Error { get; }
    public bool Success => Error == null;
    public ConversationReply(string personId, string text, string error = null)
    { PersonId = personId; Text = text; Error = error; }
}

[Serializable]
public sealed class NpcMessage
{
    public string role;
    public string content;
    public NpcMessage(string role, string content) { this.role = role; this.content = content; }
}
