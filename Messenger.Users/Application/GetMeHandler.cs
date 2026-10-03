using Mediator;
using Messenger.Users.Domain;

namespace Messenger.Users.Application;

public sealed class GetMeHandler
    : IRequestHandler<GetMeQuery, MeResult>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IUserProfileRepository _profileRepository;

    public GetMeHandler(
        IAccountRepository accountRepository,
        IUserProfileRepository profileRepository)
    {
        _accountRepository = accountRepository;
        _profileRepository = profileRepository;
    }


    public async ValueTask<MeResult> Handle(
        GetMeQuery request,
        CancellationToken cancellationToken)
    {
        var account = await _accountRepository
            .Get(request.AccountId, cancellationToken);

        if (account == null)
            throw new AccountNotFoundException(request.AccountId);

        var profile = await _profileRepository
            .Get(account.Id, cancellationToken);

        return new MeResult(
            account.Id,
            account.Type,
            account.IsBot,
            account.Email?.Value,
            account.OwnerId,
            profile == null ? null : ProfileResult.From(profile));
    }
}
