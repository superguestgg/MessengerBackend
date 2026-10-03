using Mediator;
using Messenger.Users.Domain;

namespace Messenger.Users.Application;

public sealed class GetBotsHandler
    : IRequestHandler<GetBotsQuery, IReadOnlyList<BotResult>>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IUserProfileRepository _profileRepository;

    public GetBotsHandler(
        IAccountRepository accountRepository,
        IUserProfileRepository profileRepository)
    {
        _accountRepository = accountRepository;
        _profileRepository = profileRepository;
    }


    public async ValueTask<IReadOnlyList<BotResult>> Handle(
        GetBotsQuery request,
        CancellationToken cancellationToken)
    {
        var bots = await _accountRepository
            .GetBotsByOwner(request.OwnerId, cancellationToken);

        var profiles = await _profileRepository
            .GetMany(bots.Select(x => x.Id).ToArray(), cancellationToken);

        var names = profiles.ToDictionary(x => x.UserId, x => x.DisplayName.Value);

        return bots
            .Select(x => new BotResult(
                x.Id,
                names.GetValueOrDefault(x.Id),
                x.CreatedAt))
            .ToArray();
    }
}
