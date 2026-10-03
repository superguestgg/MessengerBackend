using Mediator;

namespace Messenger.Users.Application;

public sealed record AuthenticateAccessTokenQuery(
    string Token
) : IRequest<AuthenticatedAccount?>;
