using Mediator;

namespace Messenger.Chats.Application;

public sealed record CreateGroupChatCommand(
    Guid RequesterId,
    string Title,
    IReadOnlyList<Guid> MemberIds
) : IRequest<CreateChatResult>;
