using Mediator;
using Messenger.Files.Domain;

namespace Messenger.Files.Application;

public sealed class DeleteAbandonedFilesHandler
    : IRequestHandler<DeleteAbandonedFilesCommand, int>
{
    private const int BatchSize = 100;

    private readonly IStoredFileRepository _fileRepository;
    private readonly IFileStorage _storage;
    private readonly TimeProvider _time;

    public DeleteAbandonedFilesHandler(
        IStoredFileRepository fileRepository,
        IFileStorage storage,
        TimeProvider time)
    {
        _fileRepository = fileRepository;
        _storage = storage;
        _time = time;
    }


    public async ValueTask<int> Handle(
        DeleteAbandonedFilesCommand request,
        CancellationToken cancellationToken)
    {
        var cutoff = _time.GetUtcNow().UtcDateTime - StoredFile.UnattachedLifetime;

        var deleted = 0;

        while (true)
        {
            var files = await _fileRepository.GetUnattachedCreatedBefore(cutoff, BatchSize, cancellationToken);

            // Metadata goes last: if the process stops halfway, the next run finds the file again.
            foreach (var file in files)
            {
                await _storage.Delete(file.Id, cancellationToken);

                await _fileRepository.Delete(file.Id, cancellationToken);
            }

            deleted += files.Count;

            if (files.Count < BatchSize)
                return deleted;
        }
    }
}
