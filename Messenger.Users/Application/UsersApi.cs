using Messenger.Users.Contracts;
using Messenger.Users.Domain;

namespace Messenger.Users.Application;

public sealed class UsersApi : IUsersApi
{
    private readonly IAccountRepository _accountRepository;
    private readonly IUserProfileRepository _profileRepository;

    public UsersApi(
        IAccountRepository accountRepository,
        IUserProfileRepository profileRepository)
    {
        _accountRepository = accountRepository;
        _profileRepository = profileRepository;
    }

    public async Task<IReadOnlyList<AccountInfo>> GetAccounts(
        IReadOnlyCollection<Guid> accountIds,
        CancellationToken cancellationToken = default)
    {
        if (accountIds.Count == 0)
            return [];

        var accounts = await _accountRepository
            .GetMany(accountIds, cancellationToken);

        var profiles = await _profileRepository
            .GetMany(accountIds, cancellationToken);

        var names = profiles.ToDictionary(x => x.UserId, x => x.DisplayName.Value);

        return accounts
            .Select(x => new AccountInfo(
                x.Id,
                x.IsBot,
                x.OwnerId,
                x.IsActive,
                names.GetValueOrDefault(x.Id)))
            .ToArray();
    }
}
