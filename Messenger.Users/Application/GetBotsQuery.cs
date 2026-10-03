using Mediator;

namespace Messenger.Users.Application;

public sealed record GetBotsQuery(
    Guid OwnerId
) : IRequest<IReadOnlyList<BotResult>>;
