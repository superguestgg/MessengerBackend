using Mediator;
using Messenger.Users.Domain;

namespace Messenger.Users.Application;

public sealed class ReissueBotTokenHandler
    : IRequestHandler<ReissueBotTokenCommand, BotTokenResult>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IAccessTokenRepository _tokenRepository;
    private readonly IAccessTokenSecrets _secrets;

    public ReissueBotTokenHandler(
        IAccountRepository accountRepository,
        IAccessTokenRepository tokenRepository,
        IAccessTokenSecrets secrets)
    {
        _accountRepository = accountRepository;
        _tokenRepository = tokenRepository;
        _secrets = secrets;
    }


    public async ValueTask<BotTokenResult> Handle(
        ReissueBotTokenCommand request,
        CancellationToken cancellationToken)
    {
        var bot = await _accountRepository
            .Get(request.BotId, cancellationToken);

        if (bot == null)
            throw new AccountNotFoundException(request.BotId);

        var activeTokens = await _tokenRepository
            .GetActiveByAccount(bot.Id, cancellationToken);


        var (secret, hash) = _secrets.Generate();

        var token = bot.IssueBotToken(request.RequesterId, activeTokens, hash);


        foreach (var revoked in activeTokens)
            await _tokenRepository.Update(revoked, cancellationToken);

        await _tokenRepository.Add(token, cancellationToken);


        return new BotTokenResult(bot.Id, secret);
    }
}
