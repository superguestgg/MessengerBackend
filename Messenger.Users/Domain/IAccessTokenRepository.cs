namespace Messenger.Users.Domain;

public interface IAccessTokenRepository
{
    Task<AccessToken?> Get(Guid id, CancellationToken cancellationToken = default);

    Task<AccessToken?> GetByHash(string tokenHash, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AccessToken>> GetActiveByAccount(Guid accountId, CancellationToken cancellationToken = default);

    Task Add(AccessToken token, CancellationToken cancellationToken = default);

    Task Update(AccessToken token, CancellationToken cancellationToken = default);
}
