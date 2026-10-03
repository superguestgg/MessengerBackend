namespace Messenger.Chats.Domain;

public sealed record ChatActivity(
    long LastMessageSeq,
    DateTime LastMessageAt
);
