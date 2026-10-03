namespace Messenger.Chats.Domain;

public sealed record MessageSent(
    Guid ChatId,
    long Seq,
    Guid AuthorId
) : IDomainEvent;
