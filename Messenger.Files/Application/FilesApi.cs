using Messenger.Files.Contracts;
using Messenger.Files.Domain;

namespace Messenger.Files.Application;

public sealed class FilesApi : IFilesApi
{
    private readonly IStoredFileRepository _fileRepository;
    private readonly IFileStorage _storage;
    private readonly TimeProvider _time;

    public FilesApi(
        IStoredFileRepository fileRepository,
        IFileStorage storage,
        TimeProvider time)
    {
        _fileRepository = fileRepository;
        _storage = storage;
        _time = time;
    }

    public async Task<IReadOnlyList<FileDescription>> GetOwnFiles(
        Guid ownerId,
        IReadOnlyCollection<Guid> fileIds,
        CancellationToken cancellationToken = default)
    {
        if (fileIds.Count == 0)
            return [];

        var files = await _fileRepository
            .GetMany(fileIds, cancellationToken);

        return files
            .Where(x => x.IsOwnedBy(ownerId))
            .Select(x => new FileDescription(
                x.Id,
                x.FileName.Value,
                x.ContentType.Value,
                x.Size))
            .ToArray();
    }

    public async Task MarkAttached(
        IReadOnlyCollection<Guid> fileIds,
        CancellationToken cancellationToken = default)
    {
        if (fileIds.Count == 0)
            return;

        var files = await _fileRepository
            .GetMany(fileIds, cancellationToken);

        var now = _time.GetUtcNow().UtcDateTime;

        foreach (var file in files.Where(x => x.AttachedAt == null))
        {
            file.MarkAttached(now);

            await _fileRepository.Update(file, cancellationToken);
        }
    }

    public async Task<Stream?> Open(
        Guid fileId,
        CancellationToken cancellationToken = default)
    {
        var file = await _fileRepository
            .Get(fileId, cancellationToken);

        if (file == null)
            return null;

        return await _storage.Open(file.Id, cancellationToken);
    }
}
