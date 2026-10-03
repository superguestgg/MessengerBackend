namespace Messenger.Users.Contracts;

public sealed record AccountInfo(
    Guid AccountId,
    bool IsBot,
    Guid? OwnerId,
    bool IsActive,
    string? DisplayName
);
