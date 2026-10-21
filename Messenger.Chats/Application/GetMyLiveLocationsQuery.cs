using Mediator;

namespace Messenger.Chats.Application;

// What the account is broadcasting right now, in every chat: lets any device show and stop it.
public sealed record GetMyLiveLocationsQuery(
    Guid UserId
) : IRequest<IReadOnlyList<LiveLocationResult>>;
