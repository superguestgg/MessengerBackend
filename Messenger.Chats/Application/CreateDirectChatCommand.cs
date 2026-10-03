using Mediator;

namespace Messenger.Chats.Application;

public sealed record CreateDirectChatCommand(
    Guid RequesterId,
    Guid UserId
) : IRequest<CreateChatResult>;
