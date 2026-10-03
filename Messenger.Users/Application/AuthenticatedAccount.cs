using Messenger.Users.Domain;

namespace Messenger.Users.Application;

public sealed record AuthenticatedAccount(
    Guid AccountId,
    AccountType Type
);
