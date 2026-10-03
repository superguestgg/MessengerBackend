namespace Messenger.Users.Application;

public sealed record CreateBotResult(
    Guid BotId,
    string Token
);
