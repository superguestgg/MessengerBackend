namespace Messenger.Chats.Domain;

public sealed class ParticipantNotFoundException : DomainException
{
    public ParticipantNotFoundException(Guid userId)
        : base($"Account '{userId}' was not found.")
    {
        UserId = userId;
    }

    public Guid UserId { get; }
}
