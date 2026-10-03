using System.Text.RegularExpressions;
using Messenger.Users.Domain;
using MongoDB.Bson;
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

    public async Task<IReadOnlyList<UserProfile>> GetMany(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken = default)
    {
        return await _collection
            .Find(Builders<UserProfile>.Filter.In(x => x.UserId, userIds))
            .ToListAsync(cancellationToken);
    }

    // A regex scan without an index: fine while there are few users. At scale this needs
    // a normalized name field or a search index, which would change the document shape.
    public async Task<IReadOnlyList<UserProfile>> SearchByDisplayName(
        string prefix,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var escaped = Regex.Replace(prefix, @"[\\^$.|?*+()\[\]{}]", @"\$0");

        // People type "е" for "ё": "петр" should find "Пётр".
        var pattern = @"(^|\s)" + Regex.Replace(escaped, "[еёЕЁ]", "[её]");

        return await _collection
            .Find(Builders<UserProfile>.Filter.Regex(x => x.DisplayName, new BsonRegularExpression(pattern, "i")))
            .Sort(Builders<UserProfile>.Sort.Ascending(x => x.DisplayName))
            .Limit(limit)
            .ToListAsync(cancellationToken);
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
