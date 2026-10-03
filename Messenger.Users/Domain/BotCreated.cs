namespace Messenger.Users.Domain;

public sealed record BotCreated(
    Guid BotId,
    Guid OwnerId,
    DisplayName DisplayName
) : IDomainEvent;
