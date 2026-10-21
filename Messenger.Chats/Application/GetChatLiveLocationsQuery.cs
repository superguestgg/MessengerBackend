using Mediator;

namespace Messenger.Chats.Application;

// Live locations that are still being broadcast in the chat.
public sealed record GetChatLiveLocationsQuery(
    Guid UserId,
    Guid ChatId
) : IRequest<IReadOnlyList<LiveLocationResult>>;
