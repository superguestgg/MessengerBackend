namespace Messenger.Users.Application;

public sealed record ProfileResult(
    Guid UserId,
    string DisplayName,
    Guid? AvatarId,
    string? Bio,
    DateTime UpdatedAt
);
