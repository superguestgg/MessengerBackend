using Messenger.Chats.Domain;
using Messenger.Users.Contracts;

namespace Messenger.Chats.Application;

// Turns accounts from the Users module into the Chats module's own model.
public sealed class ParticipantLookup
{
    private readonly IUsersApi _usersApi;

    public ParticipantLookup(IUsersApi usersApi)
    {
        _usersApi = usersApi;
    }

    // Throws for unknown or inactive accounts.
    public async Task<IReadOnlyList<ChatParticipant>> GetActive(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken)
    {
        var accounts = await _usersApi.GetAccounts(userIds, cancellationToken);

        var byId = accounts
            .Where(x => x.IsActive)
            .ToDictionary(x => x.AccountId);

        return userIds
            .Select(id => byId.TryGetValue(id, out var account)
                ? new ChatParticipant(account.AccountId, account.IsBot, account.OwnerId)
                : throw new ParticipantNotFoundException(id))
            .ToArray();
    }

    public async Task<ChatParticipant> GetActive(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var participants = await GetActive([userId], cancellationToken);

        return participants[0];
    }
}
