using Messenger.Users.Domain;
using MongoDB.Driver;

namespace Messenger.Users.Infrastructure;

public static class UsersMongoInitializer
{
    public static async Task EnsureIndexes(
        IMongoDatabase database,
        CancellationToken cancellationToken = default)
    {
        var accounts = database.GetCollection<Account>(AccountRepository.CollectionName);

        var emailIndex = new CreateIndexModel<Account>(
            Builders<Account>.IndexKeys.Ascending(x => x.Email),
            new CreateIndexOptions { Unique = true, Name = "ux_users_email" });

        await accounts.Indexes.CreateOneAsync(
            emailIndex,
            cancellationToken: cancellationToken);
    }
}
