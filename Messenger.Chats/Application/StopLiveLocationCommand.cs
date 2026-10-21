using Mediator;

namespace Messenger.Chats.Application;

public sealed record StopLiveLocationCommand(
    Guid UserId,
    Guid ChatId,
    long Seq
) : IRequest;
