using Mediator;
using Messenger.Files.Domain;

namespace Messenger.Files.Application;

public sealed class UploadFileHandler
    : IRequestHandler<UploadFileCommand, UploadFileResult>
{
    private readonly IStoredFileRepository _fileRepository;
    private readonly IFileStorage _storage;
    private readonly TimeProvider _time;

    public UploadFileHandler(
        IStoredFileRepository fileRepository,
        IFileStorage storage,
        TimeProvider time)
    {
        _fileRepository = fileRepository;
        _storage = storage;
        _time = time;
    }


    public async ValueTask<UploadFileResult> Handle(
        UploadFileCommand request,
        CancellationToken cancellationToken)
    {
        var file = StoredFile.Upload(
            request.OwnerId,
            new FileName(request.FileName),
            MediaType.OrUnknown(request.ContentType),
            request.Size,
            _time.GetUtcNow().UtcDateTime);

        // Contents first: a file becomes visible only once its metadata is written.
        await _storage.Save(file.Id, file.FileName.Value, request.Content, cancellationToken);

        await _fileRepository.Add(file, cancellationToken);


        return new UploadFileResult(
            file.Id,
            file.FileName.Value,
            file.ContentType.Value,
            file.Size);
    }
}
