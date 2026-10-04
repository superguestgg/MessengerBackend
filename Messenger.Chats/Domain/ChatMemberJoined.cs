namespace Messenger.Chats.Domain;

// Raised for every member of a new chat and for each member added later.
public sealed record ChatMemberJoined(
    Guid ChatId,
    Guid UserId
) : IDomainEvent;
