using Messenger.Users.Domain;

namespace Messenger.Users.Application;

public sealed record ProfileResult(
    Guid UserId,
    string DisplayName,
    Guid? AvatarId,
    string? Bio,
    DateTime UpdatedAt
)
{
    public static ProfileResult From(UserProfile profile)
    {
        return new ProfileResult(
            profile.UserId,
            profile.DisplayName.Value,
            profile.AvatarId,
            profile.Bio,
            profile.UpdatedAt);
    }
}
