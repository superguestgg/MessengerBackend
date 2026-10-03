using Mediator;
using Messenger.Users.Domain;

namespace Messenger.Users.Application;

public sealed class IssueAccessTokenHandler
    : IRequestHandler<IssueAccessTokenCommand, IssuedAccessTokenResult>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IAccessTokenRepository _tokenRepository;
    private readonly IAccessTokenSecrets _secrets;

    public IssueAccessTokenHandler(
        IAccountRepository accountRepository,
        IAccessTokenRepository tokenRepository,
        IAccessTokenSecrets secrets)
    {
        _accountRepository = accountRepository;
        _tokenRepository = tokenRepository;
        _secrets = secrets;
    }


    public async ValueTask<IssuedAccessTokenResult> Handle(
        IssueAccessTokenCommand request,
        CancellationToken cancellationToken)
    {
        var account = await _accountRepository
            .Get(request.AccountId, cancellationToken);

        if (account == null)
            throw new AccountNotFoundException(request.AccountId);


        var (secret, hash) = _secrets.Generate();

        var token = account.IssuePersonalToken(
            new AccessTokenName(request.Name),
            hash);


        await _tokenRepository.Add(token, cancellationToken);


        return new IssuedAccessTokenResult(
            token.Id,
            token.Name.Value,
            secret,
            token.CreatedAt);
    }
}
