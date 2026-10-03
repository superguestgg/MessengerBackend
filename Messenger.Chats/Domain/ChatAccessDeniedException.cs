namespace Messenger.Chats.Domain;

public sealed class ChatAccessDeniedException : DomainException
{
    public ChatAccessDeniedException(string message)
        : base(message)
    {
    }
}
