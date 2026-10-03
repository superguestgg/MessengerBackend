using Mediator;
using Messenger.Users.Domain;

namespace Messenger.Users.Application;

public sealed class RegisterUserHandler 
    : IRequestHandler<RegisterUserCommand, RegisterUserResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserProfileRepository _profileRepository;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterUserHandler(
        IUserRepository userRepository,
        IUserProfileRepository profileRepository,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _profileRepository = profileRepository;
        _passwordHasher = passwordHasher;
    }


    public async ValueTask<RegisterUserResult> Handle(
        RegisterUserCommand request,
        CancellationToken cancellationToken)
    {
        var exists = await _userRepository
            .GetByEmail(request.Email);

        if (exists != null)
            throw new Exception("Email already exists");


        var user = User.Create(
            request.Email,
            _passwordHasher.Hash(request.Password));


        var profile = UserProfile.Create(
            user.Id,
            request.DisplayName);


        await _userRepository.Add(user);

        await _profileRepository.Add(profile);


        return new RegisterUserResult(user.Id);
    }
}