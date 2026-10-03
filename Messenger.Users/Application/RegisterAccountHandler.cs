using Mediator;
using Messenger.Users.Domain;

namespace Messenger.Users.Application;

public sealed class RegisterAccountHandler
    : IRequestHandler<RegisterAccountCommand, RegisterAccountResult>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPublisher _publisher;

    public RegisterAccountHandler(
        IAccountRepository accountRepository,
        IPasswordHasher passwordHasher,
        IPublisher publisher)
    {
        _accountRepository = accountRepository;
        _passwordHasher = passwordHasher;
        _publisher = publisher;
    }


    public async ValueTask<RegisterAccountResult> Handle(
        RegisterAccountCommand request,
        CancellationToken cancellationToken)
    {
        var email = new Email(request.Email);

        var exists = await _accountRepository
            .GetByEmail(email, cancellationToken);

        if (exists != null)
            throw new EmailAlreadyTakenException(email);


        var account = Account.Register(
            email,
            _passwordHasher.Hash(request.Password));


        await _accountRepository.Add(account, cancellationToken);

        await _publisher.PublishDomainEvents(account, cancellationToken);


        return new RegisterAccountResult(account.Id);
    }
}
