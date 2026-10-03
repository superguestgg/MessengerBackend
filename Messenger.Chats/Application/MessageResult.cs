namespace Messenger.Chats.Application;

public sealed record MessageResult(
    Guid MessageId,
    Guid ChatId,
    long Seq,
    Guid AuthorId,
    string? AuthorName,
    bool AuthorIsBot,
    string Text,
    long? ReplyToSeq,
    DateTime CreatedAt
);
