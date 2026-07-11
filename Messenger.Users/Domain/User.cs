namespace Messenger.Users.Domain;

public class User
{
    private User()
    {
    }


    public static User Create(
        string email,
        string passwordHash)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = passwordHash,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
    }
    
    public Guid Id { get; private set; }

    public string Email { get; private set; }

    public string PasswordHash { get; private set; }

    public UserStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    // методы
}