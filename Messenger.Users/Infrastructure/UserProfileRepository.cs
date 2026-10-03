using Messenger.Users.Domain;
using MongoDB.Driver;

namespace Messenger.Users.Infrastructure;

public sealed class UserProfileRepository : IUserProfileRepository
{
    public const string CollectionName = "user_profiles";

    private readonly IMongoCollection<UserProfile> _collection;

    public UserProfileRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<UserProfile>(CollectionName);
    }

    public async Task<UserProfile?> Get(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _collection
            .Find(x => x.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task Save(UserProfile profile, CancellationToken cancellationToken = default)
    {
        await _collection.ReplaceOneAsync(
            x => x.UserId == profile.UserId,
            profile,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);
    }
}
