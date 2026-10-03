namespace Messenger.Users.Domain;

public sealed class AccountNotFoundException : DomainException
{
    public AccountNotFoundException(Guid accountId)
        : base($"Account '{accountId}' was not found.")
    {
        AccountId = accountId;
    }

    public Guid AccountId { get; }
}
