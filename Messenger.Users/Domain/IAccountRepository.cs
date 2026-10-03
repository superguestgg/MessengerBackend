namespace Messenger.Users.Domain;

public interface IAccountRepository
{
    Task<Account?> Get(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Account>> GetMany(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);

    Task<Account?> GetByEmail(Email email, CancellationToken cancellationToken = default);

    // Deleted bots are excluded.
    Task<IReadOnlyList<Account>> GetBotsByOwner(Guid ownerId, CancellationToken cancellationToken = default);

    Task Add(Account account, CancellationToken cancellationToken = default);

    Task Update(Account account, CancellationToken cancellationToken = default);
}
