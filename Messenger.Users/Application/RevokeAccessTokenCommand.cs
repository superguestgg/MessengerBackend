using Mediator;

namespace Messenger.Users.Application;

public sealed record RevokeAccessTokenCommand(
    Guid RequesterId,
    Guid TokenId
) : IRequest;
