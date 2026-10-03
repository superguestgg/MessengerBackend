using Messenger.Chats.Domain;

namespace Messenger.Chats.Application;

public sealed record ChatResult(
    Guid ChatId,
    ChatType Type,
    string? Title,
    IReadOnlyList<ChatMemberResult> Members,
    long LastMessageSeq,
    DateTime? LastMessageAt,
    DateTime CreatedAt
);

public sealed record ChatMemberResult(
    Guid UserId,
    string? DisplayName,
    bool IsBot,
    ChatRole Role,
    DateTime JoinedAt
);
