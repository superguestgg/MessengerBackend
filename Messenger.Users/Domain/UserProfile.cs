using MongoDB.Bson.Serialization.Attributes;

namespace Messenger.Users.Domain;

public class UserProfile
{
    private UserProfile()
    {
    }


    public static UserProfile Create(
        Guid userId,
        string displayName)
    {
        return new UserProfile
        {
            UserId = userId,
            DisplayName = displayName,
            UpdatedAt = DateTime.UtcNow
        };
    }

    [BsonId]
    public Guid UserId { get; private set; }

    public string DisplayName { get; private set; }

    public Guid? AvatarId { get; private set; }

    public string? Bio { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    // методы
}