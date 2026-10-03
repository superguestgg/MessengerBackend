namespace Messenger.Users.Domain;

public sealed record AccountRegistered(
    Guid AccountId,
    Email Email
) : IDomainEvent;
