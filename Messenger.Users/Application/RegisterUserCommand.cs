using Mediator;

namespace Messenger.Users.Domain;

public sealed record RegisterUserCommand(
    string Email,
    string Password,
    string DisplayName
) : IRequest<RegisterUserResult>;