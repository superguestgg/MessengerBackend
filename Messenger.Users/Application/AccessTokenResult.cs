namespace Messenger.Users.Application;

// The secret is never returned after issuing.
public sealed record AccessTokenResult(
    Guid TokenId,
    string Name,
    DateTime CreatedAt
);
