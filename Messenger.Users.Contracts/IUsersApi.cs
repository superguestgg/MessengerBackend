namespace Messenger.Users.Contracts;

// What other modules may ask the Users module. Only primitives cross this boundary.
public interface IUsersApi
{
    // Unknown ids are skipped, so the result can be shorter than the input.
    Task<IReadOnlyList<AccountInfo>> GetAccounts(
        IReadOnlyCollection<Guid> accountIds,
        CancellationToken cancellationToken = default);
}
