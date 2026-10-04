using Mediator;

namespace Messenger.Files.Application;

public sealed record UploadFileCommand(
    Guid OwnerId,
    string FileName,
    string? ContentType,
    long Size,
    Stream Content
) : IRequest<UploadFileResult>;
