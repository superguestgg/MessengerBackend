using Mediator;

namespace Messenger.Users.Application;

public sealed record GetMeQuery(
    Guid AccountId
) : IRequest<MeResult>;
