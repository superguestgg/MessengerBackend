namespace Messenger.Users.Domain;

public interface IUserProfileRepository
{
    Task<UserProfile?> Get(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserProfile>> GetMany(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken = default);

    // Profiles where a word of the display name starts with the prefix, ignoring case; sorted by name.
    Task<IReadOnlyList<UserProfile>> SearchByDisplayName(string prefix, int limit, CancellationToken cancellationToken = default);

    // Insert or replace: a profile is created and edited by the same operation.
    Task Save(UserProfile profile, CancellationToken cancellationToken = default);
}
