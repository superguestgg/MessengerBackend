namespace Messenger.Users.Application;

public sealed record IssuedAccessTokenResult(
    Guid TokenId,
    string Name,
    string Token,
    DateTime CreatedAt
);
