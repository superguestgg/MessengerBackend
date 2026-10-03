using Mediator;

namespace Messenger.Users.Application;

public sealed record RegisterUserCommand(
    string Email,
    string Password,
    string DisplayName
) : IRequest<RegisterUserResult>;