namespace Messenger.Users.Domain;

// A long-lived credential for bots and for agents acting on behalf of a user.
// Only the hash of the secret is stored; the secret itself is shown once, when issued.
public class AccessToken : AggregateRoot
{
    public static readonly AccessTokenName BotTokenName = new("bot");

    private AccessToken()
    {
    }


    internal static AccessToken Issue(
        Guid accountId,
        AccessTokenName name,
        string tokenHash)
    {
        return new AccessToken
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Name = name,
            TokenHash = tokenHash,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Revoke(Guid requesterId)
    {
        if (requesterId != AccountId)
            throw new AccessTokenNotFoundException(Id);

        RevokeInternal();
    }

    internal void RevokeInternal()
    {
        RevokedAt ??= DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid AccountId { get; private set; }

    public AccessTokenName Name { get; private set; } = null!;

    public string TokenHash { get; private set; } = null!;

    public DateTime CreatedAt { get; private set; }

    public DateTime? RevokedAt { get; private set; }

    public bool IsActive => RevokedAt == null;
}
