using Mediator;
using Messenger.Users.Domain;

namespace Messenger.Users.Application;

public sealed class DeleteBotHandler
    : IRequestHandler<DeleteBotCommand>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IAccessTokenRepository _tokenRepository;

    public DeleteBotHandler(
        IAccountRepository accountRepository,
        IAccessTokenRepository tokenRepository)
    {
        _accountRepository = accountRepository;
        _tokenRepository = tokenRepository;
    }


    public async ValueTask<Unit> Handle(
        DeleteBotCommand request,
        CancellationToken cancellationToken)
    {
        var bot = await _accountRepository
            .Get(request.BotId, cancellationToken);

        if (bot == null)
            throw new AccountNotFoundException(request.BotId);

        var activeTokens = await _tokenRepository
            .GetActiveByAccount(bot.Id, cancellationToken);


        bot.DeleteBot(request.RequesterId, activeTokens);


        foreach (var revoked in activeTokens)
            await _tokenRepository.Update(revoked, cancellationToken);

        await _accountRepository.Update(bot, cancellationToken);


        return Unit.Value;
    }
}
