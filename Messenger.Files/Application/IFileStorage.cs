namespace Messenger.Files.Application;

// File contents. Metadata lives in StoredFile; both share the file's id.
public interface IFileStorage
{
    Task Save(Guid fileId, string fileName, Stream content, CancellationToken cancellationToken = default);

    // Null if there is no such file. The stream is seekable; the caller disposes it.
    Task<Stream?> Open(Guid fileId, CancellationToken cancellationToken = default);

    // A missing file is not an error.
    Task Delete(Guid fileId, CancellationToken cancellationToken = default);
}
