using Mediator;
using Messenger.Users.Domain;

namespace Messenger.Users.Application;

public sealed class SearchUsersHandler
    : IRequestHandler<SearchUsersQuery, IReadOnlyList<UserSearchResult>>
{
    // Accounts the requester must not see are dropped after the name query,
    // so it asks for more profiles than it returns.
    private const int NameSearchOverfetch = 3;

    private readonly IAccountRepository _accountRepository;
    private readonly IUserProfileRepository _profileRepository;

    public SearchUsersHandler(
        IAccountRepository accountRepository,
        IUserProfileRepository profileRepository)
    {
        _accountRepository = accountRepository;
        _profileRepository = profileRepository;
    }


    public async ValueTask<IReadOnlyList<UserSearchResult>> Handle(
        SearchUsersQuery request,
        CancellationToken cancellationToken)
    {
        var text = request.Query.Trim();

        if (Guid.TryParse(text, out var accountId))
        {
            var account = await _accountRepository
                .Get(accountId, cancellationToken);

            return await WithNames(
                [account],
                request.RequesterId,
                byExactId: true,
                cancellationToken);
        }

        if (text.Contains('@'))
        {
            var email = Email.TryCreate(text);

            if (email == null)
                return [];

            var account = await _accountRepository
                .GetByEmail(email, cancellationToken);

            return await WithNames(
                [account],
                request.RequesterId,
                byExactId: false,
                cancellationToken);
        }

        if (text.Length < SearchUsersQuery.MinLength)
            return [];

        var profiles = await _profileRepository
            .SearchByDisplayName(text, request.Limit * NameSearchOverfetch, cancellationToken);

        var accounts = (await _accountRepository
                .GetMany(profiles.Select(x => x.UserId).ToArray(), cancellationToken))
            .Where(x => x.IsFoundInSearchBy(request.RequesterId, byExactId: false))
            .ToDictionary(x => x.Id);

        return profiles
            .Where(x => accounts.ContainsKey(x.UserId))
            .Take(request.Limit)
            .Select(x => new UserSearchResult(
                x.UserId,
                x.DisplayName.Value,
                accounts[x.UserId].IsBot))
            .ToArray();
    }

    private async Task<IReadOnlyList<UserSearchResult>> WithNames(
        Account?[] accounts,
        Guid requesterId,
        bool byExactId,
        CancellationToken cancellationToken)
    {
        var found = accounts
            .Where(x => x != null && x.IsFoundInSearchBy(requesterId, byExactId))
            .Select(x => x!)
            .ToArray();

        if (found.Length == 0)
            return [];

        var profiles = await _profileRepository
            .GetMany(found.Select(x => x.Id).ToArray(), cancellationToken);

        var names = profiles.ToDictionary(x => x.UserId, x => x.DisplayName.Value);

        return found
            .Select(x => new UserSearchResult(
                x.Id,
                names.GetValueOrDefault(x.Id),
                x.IsBot))
            .ToArray();
    }
}
