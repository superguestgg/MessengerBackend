using Messenger.Users.Domain;
using MongoDB.Driver;

namespace Messenger.Users.Ifrastructure;

public sealed class UserProfileRepository : IUserProfileRepository
{
    private readonly IMongoCollection<UserProfile> _collection;

    public UserProfileRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<UserProfile>("user_profiles");
    }

    public async Task<UserProfile?> Get(Guid userId)
    {
        return await _collection
            .Find(x => x.UserId == userId)
            .FirstOrDefaultAsync();
    }

    public async Task Add(UserProfile profile)
    {
        await _collection.InsertOneAsync(profile);
    }

    public async Task Update(UserProfile profile)
    {
        await _collection.ReplaceOneAsync(
            x => x.UserId == profile.UserId,
            profile);
    }
}