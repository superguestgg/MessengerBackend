using Mediator;
using Messenger.Users.Domain;

namespace Messenger.Users.Application;

public sealed class UpdateProfileHandler
    : IRequestHandler<UpdateProfileCommand>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IUserProfileRepository _profileRepository;

    public UpdateProfileHandler(
        IAccountRepository accountRepository,
        IUserProfileRepository profileRepository)
    {
        _accountRepository = accountRepository;
        _profileRepository = profileRepository;
    }


    public async ValueTask<Unit> Handle(
        UpdateProfileCommand request,
        CancellationToken cancellationToken)
    {
        var account = await _accountRepository
            .Get(request.UserId, cancellationToken);

        if (account == null)
            throw new AccountNotFoundException(request.UserId);


        var displayName = new DisplayName(request.DisplayName);

        var profile = await _profileRepository
            .Get(request.UserId, cancellationToken);

        if (profile == null)
            profile = UserProfile.Create(request.UserId, displayName, request.Bio);
        else
            profile.Update(displayName, request.Bio);


        await _profileRepository.Save(profile, cancellationToken);


        return Unit.Value;
    }
}
