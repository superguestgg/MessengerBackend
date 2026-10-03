namespace Messenger.Chats.Domain;

public sealed class ChatConcurrencyException : DomainException
{
    public ChatConcurrencyException(Guid chatId)
        : base($"Chat '{chatId}' was changed by another request. Retry.")
    {
        ChatId = chatId;
    }

    public Guid ChatId { get; }
}
