using Mediator;
using Messenger.Chats.Domain;

namespace Messenger.Chats.Application;

public sealed record ChangeChatMemberRoleCommand(
    Guid RequesterId,
    Guid ChatId,
    Guid UserId,
    ChatRole Role
) : IRequest;
