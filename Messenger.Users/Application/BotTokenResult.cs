namespace Messenger.Users.Application;

public sealed record BotTokenResult(
    Guid BotId,
    string Token
);
