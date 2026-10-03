using Mediator;

namespace Messenger.Users.Application;

public sealed record GetProfileQuery(
    Guid UserId
) : IRequest<ProfileResult?>;
