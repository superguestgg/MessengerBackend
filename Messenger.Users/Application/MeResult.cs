using Messenger.Users.Domain;

namespace Messenger.Users.Application;

public sealed record MeResult(
    Guid AccountId,
    AccountType Type,
    bool IsBot,
    string? Email,
    Guid? OwnerId,
    ProfileResult? Profile
);
