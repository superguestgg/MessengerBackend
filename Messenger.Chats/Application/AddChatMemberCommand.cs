using Mediator;

namespace Messenger.Chats.Application;

public sealed record AddChatMemberCommand(
    Guid RequesterId,
    Guid ChatId,
    Guid UserId
) : IRequest;
