namespace Messenger.Chats.Domain;

// Also thrown to non-members, so chat ids cannot be probed.
public sealed class ChatNotFoundException : DomainException
{
    public ChatNotFoundException(Guid chatId)
        : base($"Chat '{chatId}' was not found.")
    {
        ChatId = chatId;
    }

    public Guid ChatId { get; }
}
