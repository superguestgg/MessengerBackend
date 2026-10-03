using Mediator;

namespace Messenger.Chats.Application;

public sealed record RemoveChatMemberCommand(
    Guid RequesterId,
    Guid ChatId,
    Guid UserId
) : IRequest;
