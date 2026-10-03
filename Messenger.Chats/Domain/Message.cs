namespace Messenger.Chats.Domain;

public class Message : AggregateRoot
{
    private Message()
    {
    }


    internal static Message Create(
        Guid chatId,
        long seq,
        Guid authorId,
        MessageText text,
        long? replyToSeq)
    {
        var message = new Message
        {
            Id = Guid.NewGuid(),
            ChatId = chatId,
            Seq = seq,
            AuthorId = authorId,
            Text = text,
            ReplyToSeq = replyToSeq,
            CreatedAt = DateTime.UtcNow
        };

        message.Raise(new MessageSent(chatId, seq, authorId));

        return message;
    }

    public Guid Id { get; private set; }

    public Guid ChatId { get; private set; }

    // Grows within a chat; numbers can be skipped if a send fails after allocation.
    public long Seq { get; private set; }

    // Only the id: the author's name is looked up when messages are read.
    public Guid AuthorId { get; private set; }

    public MessageText Text { get; private set; } = null!;

    public long? ReplyToSeq { get; private set; }

    public DateTime CreatedAt { get; private set; }
}
