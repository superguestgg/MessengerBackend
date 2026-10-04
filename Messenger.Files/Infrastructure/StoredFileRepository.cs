using Messenger.Files.Domain;
using MongoDB.Driver;

namespace Messenger.Files.Infrastructure;

public sealed class StoredFileRepository : IStoredFileRepository
{
    public const string CollectionName = "stored_files";

    private readonly IMongoCollection<StoredFile> _files;

    public StoredFileRepository(IMongoDatabase database)
    {
        _files = database.GetCollection<StoredFile>(CollectionName);
    }

    public async Task<StoredFile?> Get(Guid id, CancellationToken cancellationToken = default)
    {
        return await _files
            .Find(x => x.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StoredFile>> GetMany(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
            return [];

        return await _files
            .Find(Builders<StoredFile>.Filter.In(x => x.Id, ids))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StoredFile>> GetUnattachedCreatedBefore(
        DateTime cutoff,
        int limit,
        CancellationToken cancellationToken = default)
    {
        return await _files
            .Find(x => x.AttachedAt == null && x.CreatedAt < cutoff)
            .Limit(limit)
            .ToListAsync(cancellationToken);
    }

    public Task Add(StoredFile file, CancellationToken cancellationToken = default)
    {
        return _files.InsertOneAsync(file, cancellationToken: cancellationToken);
    }

    public Task Update(StoredFile file, CancellationToken cancellationToken = default)
    {
        return _files.ReplaceOneAsync(
            x => x.Id == file.Id,
            file,
            cancellationToken: cancellationToken);
    }

    public Task Delete(Guid id, CancellationToken cancellationToken = default)
    {
        return _files.DeleteOneAsync(x => x.Id == id, cancellationToken);
    }
}
