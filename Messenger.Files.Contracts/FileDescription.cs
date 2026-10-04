namespace Messenger.Files.Contracts;

public sealed record FileDescription(
    Guid FileId,
    string FileName,
    string ContentType,
    long Size
);
