namespace Messenger.Chats.Domain;

// Raised when a member leaves a group or is removed from it.
public sealed record ChatMemberLeft(
    Guid ChatId,
    Guid UserId
) : IDomainEvent;
