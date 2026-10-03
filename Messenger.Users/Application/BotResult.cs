namespace Messenger.Users.Application;

public sealed record BotResult(
    Guid BotId,
    string? DisplayName,
    DateTime CreatedAt
);
