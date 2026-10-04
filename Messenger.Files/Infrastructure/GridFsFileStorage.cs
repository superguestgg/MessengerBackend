using Messenger.Files.Application;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace Messenger.Files.Infrastructure;

// Contents in GridFS, keyed by the same Guid as the file's metadata.
public sealed class GridFsFileStorage : IFileStorage
{
    // Collections file_contents.files and file_contents.chunks.
    public const string BucketName = "file_contents";

    private readonly GridFSBucket<Guid> _bucket;

    public GridFsFileStorage(IMongoDatabase database)
    {
        _bucket = new GridFSBucket<Guid>(database, new GridFSBucketOptions
        {
            BucketName = BucketName
        });
    }

    public Task Save(Guid fileId, string fileName, Stream content, CancellationToken cancellationToken = default)
    {
        return _bucket.UploadFromStreamAsync(
            fileId,
            fileName,
            content,
            cancellationToken: cancellationToken);
    }

    public async Task<Stream?> Open(Guid fileId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _bucket.OpenDownloadStreamAsync(
                fileId,
                new GridFSDownloadOptions { Seekable = true },
                cancellationToken);
        }
        catch (GridFSFileNotFoundException)
        {
            return null;
        }
    }

    public async Task Delete(Guid fileId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _bucket.DeleteAsync(fileId, cancellationToken);
        }
        catch (GridFSFileNotFoundException)
        {
        }
    }
}
