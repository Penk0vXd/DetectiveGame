using System;
using System.Threading;
using System.Threading.Tasks;

public interface ILocalNpcModel
{
    Task<string> ReplyAsync(NpcMessage[] messages, CancellationToken cancellation);
}

// PROTOTYPE CODE: transcript от claims; няма зависимости към evidence/case state.
public sealed class ConversationSession : IDisposable
{
    private readonly NpcPromptBuilder prompt;
    private readonly ILocalNpcModel model;
    private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
    private bool disposed;
    public ConversationState State { get; }
    public bool IsBusy { get; private set; }
    public string DisplayName => prompt.DisplayName;

    public ConversationSession(NpcProfile profile, ConversationChannel channel, ILocalNpcModel model)
    {
        prompt = new NpcPromptBuilder(profile);
        this.model = model ?? throw new ArgumentNullException(nameof(model));
        State = new ConversationState(prompt.PersonId, channel);
    }

    public async Task<ConversationReply> SendAsync(ConversationRequest request)
    {
        string error = Validate(request);
        if (error != null) return new ConversationReply(State.PersonId, null, error);
        IsBusy = true;
        var player = new ConversationTurn("player", request.Text, ConversationTurnStatus.Pending);
        State.Add(player);
        try
        {
            string response = await model.ReplyAsync(prompt.Build(State, request.Text), lifetime.Token);
            lifetime.Token.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(response) || response.Length > NpcPromptBuilder.MaxReplyCharacters)
                throw new InvalidOperationException("Моделът върна празен или твърде дълъг отговор.");
            player.Status = ConversationTurnStatus.Accepted;
            State.Add(new ConversationTurn(State.PersonId, response, ConversationTurnStatus.Accepted));
            return new ConversationReply(State.PersonId, response);
        }
        catch (Exception exception)
        {
            player.Status = disposed ? ConversationTurnStatus.Cancelled : ConversationTurnStatus.Failed;
            player.Error = disposed ? "Разговорът е затворен." : exception is OperationCanceledException
                ? "Времето за отговор изтече. Опитайте отново." : "Няма достъпен отговор от локалния модел. Опитайте отново.";
            return new ConversationReply(State.PersonId, null, player.Error);
        }
        finally
        {
            IsBusy = false;
            if (disposed) lifetime.Dispose();
        }
    }

    private string Validate(ConversationRequest request)
    {
        if (disposed) return "Разговорът е затворен.";
        if (IsBusy) return "Изчакайте текущия отговор.";
        if (request == null || request.PersonId != State.PersonId || request.Channel != State.Channel)
            return "Непознат персонаж или канал.";
        if (string.IsNullOrWhiteSpace(request.Text)) return "Напишете съобщение.";
        if (request.Text.Length > NpcPromptBuilder.MaxInputCharacters) return "Съобщението е твърде дълго (до 1200 знака).";
        if (request.MaterialIds.Count != 0) return "Няма проверено действие за представяне на този материал.";
        return null;
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        lifetime.Cancel();
        // При активна заявка finally освобождава token source след callback-а.
        if (!IsBusy) lifetime.Dispose();
    }
}
