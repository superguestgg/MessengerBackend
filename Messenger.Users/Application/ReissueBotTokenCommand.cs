using Mediator;

namespace Messenger.Users.Application;

public sealed record ReissueBotTokenCommand(
    Guid RequesterId,
    Guid BotId
) : IRequest<BotTokenResult>;
