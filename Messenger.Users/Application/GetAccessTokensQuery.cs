using Mediator;

namespace Messenger.Users.Application;

public sealed record GetAccessTokensQuery(
    Guid AccountId
) : IRequest<IReadOnlyList<AccessTokenResult>>;
