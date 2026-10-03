using Mediator;

namespace Messenger.Chats.Application;

public sealed record GetMessagesQuery(
    Guid UserId,
    Guid ChatId,
    long? AfterSeq,
    long? BeforeSeq,
    int Limit
) : IRequest<IReadOnlyList<MessageResult>>;
