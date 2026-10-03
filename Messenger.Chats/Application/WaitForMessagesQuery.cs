using Mediator;

namespace Messenger.Chats.Application;

// Waits until the chat has a message newer than AfterSeq that passes the filters:
// FromUserId — written by this account; ReplyToSeq — a reply to this message.
public sealed record WaitForMessagesQuery(
    Guid UserId,
    Guid ChatId,
    long AfterSeq,
    Guid? FromUserId,
    long? ReplyToSeq,
    TimeSpan Timeout
) : IRequest<WaitForMessagesResult>;
