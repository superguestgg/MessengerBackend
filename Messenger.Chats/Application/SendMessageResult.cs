namespace Messenger.Chats.Application;

public sealed record SendMessageResult(
    Guid MessageId,
    Guid ChatId,
    long Seq,
    DateTime CreatedAt
);
