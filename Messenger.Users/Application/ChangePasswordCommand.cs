using Mediator;

namespace Messenger.Users.Application;

public sealed record ChangePasswordCommand(
    Guid AccountId,
    string CurrentPassword,
    string NewPassword
) : IRequest;
