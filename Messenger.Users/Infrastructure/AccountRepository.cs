using Messenger.Users.Domain;
using MongoDB.Driver;

namespace Messenger.Users.Infrastructure;

public class AccountRepository : IAccountRepository
{
    // Kept from the time accounts were called users, so existing data and indexes stay valid.
    public const string CollectionName = "users";

    private readonly IMongoCollection<Account> _accounts;

    public AccountRepository(IMongoDatabase database)
    {
        _accounts = database.GetCollection<Account>(CollectionName);
    }

    public async Task<Account?> Get(Guid id, CancellationToken cancellationToken = default)
    {
        return await _accounts
            .Find(x => x.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Account?> GetByEmail(Email email, CancellationToken cancellationToken = default)
    {
        return await _accounts
            .Find(Builders<Account>.Filter.Eq(x => x.Email, email))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Account>> GetBotsByOwner(Guid ownerId, CancellationToken cancellationToken = default)
    {
        return await _accounts
            .Find(x => x.OwnerId == ownerId && x.Status != AccountStatus.Deleted)
            .SortBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task Add(Account account, CancellationToken cancellationToken = default)
    {
        try
        {
            await _accounts.InsertOneAsync(account, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException e)
            when (e.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            // Two concurrent registrations passed the GetByEmail check; the unique index caught it.
            throw new EmailAlreadyTakenException(account.Email!);
        }
    }

    public Task Update(Account account, CancellationToken cancellationToken = default)
    {
        return _accounts.ReplaceOneAsync(
            x => x.Id == account.Id,
            account,
            cancellationToken: cancellationToken);
    }
}
