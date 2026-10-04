namespace Messenger.Chats.Domain;

// How far a member has read a chat: every message up to and including Seq.
// Stored apart from the chat document, so frequent marks never race with member changes.
public sealed record ReadMark
{
    public ReadMark(
        Guid chatId,
        Guid userId,
        long seq)
    {
        if (seq < 0)
            throw new DomainException("A read mark cannot be negative.");

        ChatId = chatId;
        UserId = userId;
        Seq = seq;
    }

    public Guid ChatId { get; }

    public Guid UserId { get; }

    public long Seq { get; }
}
