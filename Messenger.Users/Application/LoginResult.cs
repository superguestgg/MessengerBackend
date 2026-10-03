namespace Messenger.Users.Application;

public sealed record LoginResult(
    Guid AccountId,
    string AccessToken,
    DateTime ExpiresAt
);
