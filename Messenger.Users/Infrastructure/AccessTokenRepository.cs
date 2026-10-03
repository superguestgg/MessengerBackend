using Messenger.Users.Domain;
using MongoDB.Driver;

namespace Messenger.Users.Infrastructure;

public sealed class AccessTokenRepository : IAccessTokenRepository
{
    public const string CollectionName = "access_tokens";

    private readonly IMongoCollection<AccessToken> _tokens;

    public AccessTokenRepository(IMongoDatabase database)
    {
        _tokens = database.GetCollection<AccessToken>(CollectionName);
    }

    public async Task<AccessToken?> Get(Guid id, CancellationToken cancellationToken = default)
    {
        return await _tokens
            .Find(x => x.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<AccessToken?> GetByHash(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await _tokens
            .Find(x => x.TokenHash == tokenHash)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AccessToken>> GetActiveByAccount(Guid accountId, CancellationToken cancellationToken = default)
    {
        return await _tokens
            .Find(x => x.AccountId == accountId && x.RevokedAt == null)
            .SortBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task Add(AccessToken token, CancellationToken cancellationToken = default)
    {
        return _tokens.InsertOneAsync(token, cancellationToken: cancellationToken);
    }

    public Task Update(AccessToken token, CancellationToken cancellationToken = default)
    {
        return _tokens.ReplaceOneAsync(
            x => x.Id == token.Id,
            token,
            cancellationToken: cancellationToken);
    }
}
