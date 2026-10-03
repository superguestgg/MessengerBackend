namespace Messenger.Chats.Domain;

public sealed class DirectChatAlreadyExistsException : DomainException
{
    public DirectChatAlreadyExistsException(string directKey)
        : base("A direct chat between these accounts already exists.")
    {
        DirectKey = directKey;
    }

    public string DirectKey { get; }
}
