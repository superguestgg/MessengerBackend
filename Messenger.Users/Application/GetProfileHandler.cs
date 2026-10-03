using Mediator;
using Messenger.Users.Domain;

namespace Messenger.Users.Application;

public sealed class GetProfileHandler
    : IRequestHandler<GetProfileQuery, ProfileResult?>
{
    private readonly IUserProfileRepository _profileRepository;

    public GetProfileHandler(IUserProfileRepository profileRepository)
    {
        _profileRepository = profileRepository;
    }


    public async ValueTask<ProfileResult?> Handle(
        GetProfileQuery request,
        CancellationToken cancellationToken)
    {
        var profile = await _profileRepository
            .Get(request.UserId, cancellationToken);

        if (profile == null)
            return null;

        return new ProfileResult(
            profile.UserId,
            profile.DisplayName.Value,
            profile.AvatarId,
            profile.Bio,
            profile.UpdatedAt);
    }
}
