using Mediator;

namespace Messenger.Users.Application;

public sealed record UpdateProfileCommand(
    Guid UserId,
    string DisplayName,
    string? Bio
) : IRequest;
