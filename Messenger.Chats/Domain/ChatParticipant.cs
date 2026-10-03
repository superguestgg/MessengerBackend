namespace Messenger.Chats.Domain;

// How the Chats module sees an account: only what matters for chat rules.
public sealed record ChatParticipant(
    Guid UserId,
    bool IsBot,
    Guid? OwnerId
);
