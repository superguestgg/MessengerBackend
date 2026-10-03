using Messenger.Users.Domain;
using MongoDB.Driver;

namespace Messenger.Users.Infrastructure;

public static class UsersMongoInitializer
{
    public static async Task EnsureIndexes(
        IMongoDatabase database,
        CancellationToken cancellationToken = default)
    {
        var users = database.GetCollection<User>("users");

        var emailIndex = new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Ascending(x => x.Email),
            new CreateIndexOptions { Unique = true, Name = "ux_users_email" });

        await users.Indexes.CreateOneAsync(
            emailIndex,
            cancellationToken: cancellationToken);
    }
}
