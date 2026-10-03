using Mediator;
using Messenger.Users.Domain;

namespace Messenger.Users.Application;

public sealed class AuthenticateAccessTokenHandler
    : IRequestHandler<AuthenticateAccessTokenQuery, AuthenticatedAccount?>
{
    private readonly IAccessTokenRepository _tokenRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IAccessTokenSecrets _secrets;

    public AuthenticateAccessTokenHandler(
        IAccessTokenRepository tokenRepository,
        IAccountRepository accountRepository,
        IAccessTokenSecrets secrets)
    {
        _tokenRepository = tokenRepository;
        _accountRepository = accountRepository;
        _secrets = secrets;
    }


    public async ValueTask<AuthenticatedAccount?> Handle(
        AuthenticateAccessTokenQuery request,
        CancellationToken cancellationToken)
    {
        if (!_secrets.LooksLikeToken(request.Token))
            return null;

        var token = await _tokenRepository
            .GetByHash(_secrets.Hash(request.Token), cancellationToken);

        if (token == null || !token.IsActive)
            return null;

        var account = await _accountRepository
            .Get(token.AccountId, cancellationToken);

        if (account == null || !account.IsActive)
            return null;

        return new AuthenticatedAccount(account.Id, account.Type);
    }
}
