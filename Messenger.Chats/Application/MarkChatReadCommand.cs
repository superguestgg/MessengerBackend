using Mediator;

namespace Messenger.Chats.Application;

public sealed record MarkChatReadCommand(
    Guid UserId,
    Guid ChatId,
    long Seq
) : IRequest;
