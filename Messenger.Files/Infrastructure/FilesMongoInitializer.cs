using Messenger.Files.Domain;
using MongoDB.Driver;

namespace Messenger.Files.Infrastructure;

public static class FilesMongoInitializer
{
    public static async Task EnsureIndexes(
        IMongoDatabase database,
        CancellationToken cancellationToken = default)
    {
        var files = database.GetCollection<StoredFile>(StoredFileRepository.CollectionName);

        // The cleanup looks for old files without AttachedAt; a missing field is indexed as null.
        var unattachedIndex = new CreateIndexModel<StoredFile>(
            Builders<StoredFile>.IndexKeys
                .Ascending(x => x.AttachedAt)
                .Ascending(x => x.CreatedAt),
            new CreateIndexOptions { Name = "ix_stored_files_unattached" });

        await files.Indexes.CreateOneAsync(
            unattachedIndex,
            cancellationToken: cancellationToken);
    }
}
