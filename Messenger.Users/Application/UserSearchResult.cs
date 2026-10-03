namespace Messenger.Users.Application;

public sealed record UserSearchResult(
    Guid UserId,
    string? DisplayName,
    bool IsBot
);
