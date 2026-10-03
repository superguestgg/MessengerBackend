using Mediator;

namespace Messenger.Users.Application;

public sealed record IssueAccessTokenCommand(
    Guid AccountId,
    string Name
) : IRequest<IssuedAccessTokenResult>;
