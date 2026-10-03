using Mediator;
using Messenger.Users.Domain;

namespace Messenger.Users.Application;

// A bot gets its name at creation, so its profile is filled in right away
// instead of waiting for a separate request like a user's.
public sealed class CreateBotProfileHandler
    : INotificationHandler<BotCreated>
{
    private readonly IUserProfileRepository _profileRepository;

    public CreateBotProfileHandler(IUserProfileRepository profileRepository)
    {
        _profileRepository = profileRepository;
    }


    public async ValueTask Handle(
        BotCreated notification,
        CancellationToken cancellationToken)
    {
        var profile = UserProfile.Create(
            notification.BotId,
            notification.DisplayName,
            null);

        await _profileRepository.Save(profile, cancellationToken);
    }
}
