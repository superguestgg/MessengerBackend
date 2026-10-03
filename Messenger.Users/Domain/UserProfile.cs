namespace Messenger.Users.Domain;

public class UserProfile
{
    public const int BioMaxLength = 500;

    private UserProfile()
    {
    }


    public static UserProfile Create(
        Guid userId,
        DisplayName displayName,
        string? bio)
    {
        var profile = new UserProfile { UserId = userId };

        profile.Update(displayName, bio);

        return profile;
    }

    public void Update(
        DisplayName displayName,
        string? bio)
    {
        bio = string.IsNullOrWhiteSpace(bio) ? null : bio.Trim();

        if (bio?.Length > BioMaxLength)
            throw new DomainException(
                $"Bio must be at most {BioMaxLength} characters long.");

        DisplayName = displayName;
        Bio = bio;
        UpdatedAt = DateTime.UtcNow;
    }

    public Guid UserId { get; private set; }

    public DisplayName DisplayName { get; private set; } = null!;

    public Guid? AvatarId { get; private set; }

    public string? Bio { get; private set; }

    public DateTime UpdatedAt { get; private set; }
}
