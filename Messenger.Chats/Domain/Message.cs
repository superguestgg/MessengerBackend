namespace Messenger.Chats.Domain;

public class Message : AggregateRoot
{
    // Not readonly: the Mongo driver sets it when loading a message.
    private List<Attachment> _attachments = new();

    private Message()
    {
    }


    internal static Message Create(
        Guid chatId,
        long seq,
        Guid authorId,
        MessageContent content,
        long? replyToSeq)
    {
        var message = new Message
        {
            Id = Guid.NewGuid(),
            ChatId = chatId,
            Seq = seq,
            AuthorId = authorId,
            Text = content.Text,
            ReplyToSeq = replyToSeq,
            CreatedAt = DateTime.UtcNow
        };

        message._attachments.AddRange(content.Attachments);

        message.Raise(new MessageSent(chatId, seq, authorId));

        return message;
    }

    public Guid Id { get; private set; }

    public Guid ChatId { get; private set; }

    // Grows within a chat; numbers can be skipped if a send fails after allocation.
    public long Seq { get; private set; }

    // Only the id: the author's name is looked up when messages are read.
    public Guid AuthorId { get; private set; }

    // Null when the message is only attachments, e.g. a voice message.
    public MessageText? Text { get; private set; }

    public IReadOnlyList<Attachment> Attachments => _attachments;

    public long? ReplyToSeq { get; private set; }

    public DateTime CreatedAt { get; private set; }
}
