using Messenger.Users.Domain;
using MongoDB.Driver;

namespace Messenger.Users.Infrastructure;

public static class UsersMongoInitializer
{
    private const string EmailIndexName = "ux_users_email";

    public static async Task EnsureIndexes(
        IMongoDatabase database,
        CancellationToken cancellationToken = default)
    {
        var accounts = database.GetCollection<Account>(AccountRepository.CollectionName);

        await MigrateEmailIndexToPartial(accounts, cancellationToken);

        var emailIndex = new CreateIndexModel<Account>(
            Builders<Account>.IndexKeys.Ascending(x => x.Email),
            new CreateIndexOptions<Account>
            {
                Unique = true,
                Name = EmailIndexName,
                PartialFilterExpression = Builders<Account>.Filter.Exists(x => x.Email)
            });

        var ownerIndex = new CreateIndexModel<Account>(
            Builders<Account>.IndexKeys.Ascending(x => x.OwnerId),
            new CreateIndexOptions<Account>
            {
                Name = "ix_users_owner",
                PartialFilterExpression = Builders<Account>.Filter.Exists(x => x.OwnerId)
            });

        await accounts.Indexes.CreateManyAsync(
            [emailIndex, ownerIndex],
            cancellationToken);


        var tokens = database.GetCollection<AccessToken>(AccessTokenRepository.CollectionName);

        var hashIndex = new CreateIndexModel<AccessToken>(
            Builders<AccessToken>.IndexKeys.Ascending(x => x.TokenHash),
            new CreateIndexOptions { Unique = true, Name = "ux_access_tokens_hash" });

        var accountIndex = new CreateIndexModel<AccessToken>(
            Builders<AccessToken>.IndexKeys.Ascending(x => x.AccountId),
            new CreateIndexOptions { Name = "ix_access_tokens_account" });

        await tokens.Indexes.CreateManyAsync(
            [hashIndex, accountIndex],
            cancellationToken);
    }

    // The email index used to cover every account. Bots have no email, and a plain
    // unique index treats all missing emails as one value, so it is rebuilt as partial.
    private static async Task MigrateEmailIndexToPartial(
        IMongoCollection<Account> accounts,
        CancellationToken cancellationToken)
    {
        using var cursor = await accounts.Indexes.ListAsync(cancellationToken);

        var indexes = await cursor.ToListAsync(cancellationToken);

        var emailIndex = indexes.FirstOrDefault(x => x["name"] == EmailIndexName);

        if (emailIndex != null && !emailIndex.Contains("partialFilterExpression"))
            await accounts.Indexes.DropOneAsync(EmailIndexName, cancellationToken);
    }
}
