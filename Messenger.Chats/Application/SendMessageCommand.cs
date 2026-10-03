using Mediator;

namespace Messenger.Chats.Application;

public sealed record SendMessageCommand(
    Guid AuthorId,
    Guid ChatId,
    string Text,
    long? ReplyToSeq
) : IRequest<SendMessageResult>;
