using Mediator;

namespace Messenger.Chats.Application;

public sealed record GetChatsQuery(
    Guid UserId
) : IRequest<IReadOnlyList<ChatResult>>;
