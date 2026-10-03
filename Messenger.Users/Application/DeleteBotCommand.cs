using Mediator;

namespace Messenger.Users.Application;

public sealed record DeleteBotCommand(
    Guid RequesterId,
    Guid BotId
) : IRequest;
