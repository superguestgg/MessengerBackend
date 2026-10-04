using Messenger.Chats.Domain;

namespace Messenger.Chats.Application;

public sealed record MessageResult(
    Guid MessageId,
    Guid ChatId,
    long Seq,
    Guid AuthorId,
    string? AuthorName,
    bool AuthorIsBot,
    string? Text,
    IReadOnlyList<AttachmentResult> Attachments,
    long? ReplyToSeq,
    DateTime CreatedAt
);

public sealed record AttachmentResult(
    Guid FileId,
    AttachmentKind Kind,
    string FileName,
    string ContentType,
    long Size,
    int? DurationSeconds,
    TranscriptResult? Transcript
);

public sealed record TranscriptResult(
    TranscriptStatus Status,
    string? Text
);
