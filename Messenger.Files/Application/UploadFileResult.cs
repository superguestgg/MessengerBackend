namespace Messenger.Files.Application;

public sealed record UploadFileResult(
    Guid FileId,
    string FileName,
    string ContentType,
    long Size
);
