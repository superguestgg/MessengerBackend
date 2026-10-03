using Mediator;

namespace Messenger.Users.Application;

public sealed record CreateBotCommand(
    Guid OwnerId,
    string DisplayName
) : IRequest<CreateBotResult>;
