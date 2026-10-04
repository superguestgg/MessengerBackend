namespace Messenger.Files.Contracts;

// What other modules may ask the Files module. Only primitives cross this boundary.
public interface IFilesApi
{
    // Files uploaded by this account. Unknown ids and other accounts' files are skipped,
    // so the result can be shorter than the input.
    Task<IReadOnlyList<FileDescription>> GetOwnFiles(
        Guid ownerId,
        IReadOnlyCollection<Guid> fileIds,
        CancellationToken cancellationToken = default);

    // Attached files are kept; the rest are removed some time after upload.
    Task MarkAttached(
        IReadOnlyCollection<Guid> fileIds,
        CancellationToken cancellationToken = default);

    // Null if the file is gone. The stream is seekable, so downloads can serve
    // HTTP range requests; the caller disposes it.
    Task<Stream?> Open(
        Guid fileId,
        CancellationToken cancellationToken = default);
}
