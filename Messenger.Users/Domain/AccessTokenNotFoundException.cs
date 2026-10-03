namespace Messenger.Users.Domain;

public sealed class AccessTokenNotFoundException : DomainException
{
    public AccessTokenNotFoundException(Guid tokenId)
        : base($"Access token '{tokenId}' was not found.")
    {
        TokenId = tokenId;
    }

    public Guid TokenId { get; }
}
