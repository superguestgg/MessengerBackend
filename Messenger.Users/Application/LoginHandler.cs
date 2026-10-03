using Mediator;
using Messenger.Users.Domain;

namespace Messenger.Users.Application;

public sealed class LoginHandler
    : IRequestHandler<LoginCommand, LoginResult>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtIssuer _jwtIssuer;

    public LoginHandler(
        IAccountRepository accountRepository,
        IPasswordHasher passwordHasher,
        IJwtIssuer jwtIssuer)
    {
        _accountRepository = accountRepository;
        _passwordHasher = passwordHasher;
        _jwtIssuer = jwtIssuer;
    }


    public async ValueTask<LoginResult> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        Email email;

        try
        {
            email = new Email(request.Email);
        }
        catch (DomainException)
        {
            throw new InvalidCredentialsException();
        }

        var account = await _accountRepository
            .GetByEmail(email, cancellationToken);

        // One error for every failure, so the response does not reveal which emails exist.
        if (account == null
            || !account.CanSignInWithPassword
            || !_passwordHasher.Verify(request.Password, account.PasswordHash!))
            throw new InvalidCredentialsException();


        var (token, expiresAt) = _jwtIssuer.Issue(account);

        return new LoginResult(account.Id, token, expiresAt);
    }
}
