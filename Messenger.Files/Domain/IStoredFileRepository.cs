namespace Messenger.Files.Domain;

public interface IStoredFileRepository
{
    Task<StoredFile?> Get(Guid id, CancellationToken cancellationToken = default);

    // Unknown ids are skipped.
    Task<IReadOnlyList<StoredFile>> GetMany(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StoredFile>> GetUnattachedCreatedBefore(
        DateTime cutoff,
        int limit,
        CancellationToken cancellationToken = default);

    Task Add(StoredFile file, CancellationToken cancellationToken = default);

    Task Update(StoredFile file, CancellationToken cancellationToken = default);

    Task Delete(Guid id, CancellationToken cancellationToken = default);
}
