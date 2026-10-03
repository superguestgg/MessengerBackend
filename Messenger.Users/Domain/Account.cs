namespace Messenger.Users.Domain;

public class Account : AggregateRoot
{
    private Account()
    {
    }


    public static Account Register(
        Email email,
        string passwordHash)
    {
        var now = DateTime.UtcNow;

        var account = new Account
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = passwordHash,
            Status = AccountStatus.Active,
            CreatedAt = now,
            UpdatedAt = now
        };

        account.Raise(new AccountRegistered(account.Id, email));

        return account;
    }

    public Guid Id { get; private set; }

    public Email Email { get; private set; } = null!;

    public string PasswordHash { get; private set; } = null!;

    public AccountStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }
}
