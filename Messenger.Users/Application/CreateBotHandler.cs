using Mediator;
using Messenger.Users.Domain;

namespace Messenger.Users.Application;

public sealed class CreateBotHandler
    : IRequestHandler<CreateBotCommand, CreateBotResult>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IAccessTokenRepository _tokenRepository;
    private readonly IAccessTokenSecrets _secrets;
    private readonly IPublisher _publisher;

    public CreateBotHandler(
        IAccountRepository accountRepository,
        IAccessTokenRepository tokenRepository,
        IAccessTokenSecrets secrets,
        IPublisher publisher)
    {
        _accountRepository = accountRepository;
        _tokenRepository = tokenRepository;
        _secrets = secrets;
        _publisher = publisher;
    }


    public async ValueTask<CreateBotResult> Handle(
        CreateBotCommand request,
        CancellationToken cancellationToken)
    {
        var owner = await _accountRepository
            .Get(request.OwnerId, cancellationToken);

        if (owner == null)
            throw new AccountNotFoundException(request.OwnerId);


        var bot = owner.CreateBot(new DisplayName(request.DisplayName));

        var (secret, hash) = _secrets.Generate();

        var token = bot.IssueBotToken(owner.Id, [], hash);


        await _accountRepository.Add(bot, cancellationToken);

        await _tokenRepository.Add(token, cancellationToken);

        await _publisher.PublishDomainEvents(bot, cancellationToken);


        return new CreateBotResult(bot.Id, secret);
    }
}
