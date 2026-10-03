using Mediator;

namespace Messenger.Chats.Application;

public sealed record GetChatQuery(
    Guid UserId,
    Guid ChatId
) : IRequest<ChatResult>;
