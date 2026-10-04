using Mediator;
using Messenger.Users.Domain;

namespace Messenger.Users.Application;

public sealed class ChangePasswordHandler
    : IRequestHandler<ChangePasswordCommand>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IPasswordHasher _passwordHasher;

    public ChangePasswordHandler(
        IAccountRepository accountRepository,
        IPasswordHasher passwordHasher)
    {
        _accountRepository = accountRepository;
        _passwordHasher = passwordHasher;
    }


    public async ValueTask<Unit> Handle(
        ChangePasswordCommand request,
        CancellationToken cancellationToken)
    {
        var account = await _accountRepository
            .Get(request.AccountId, cancellationToken);

        if (account == null)
            throw new AccountNotFoundException(request.AccountId);

        account.ChangePassword(
            passwordHash => _passwordHasher.Verify(request.CurrentPassword, passwordHash),
            _passwordHasher.Hash(request.NewPassword));


        await _accountRepository.Update(account, cancellationToken);


        return Unit.Value;
    }
}
