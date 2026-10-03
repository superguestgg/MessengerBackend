namespace Messenger.Chats.Domain;

public interface IMessageRepository
{
    Task<Message?> GetBySeq(Guid chatId, long seq, CancellationToken cancellationToken = default);

    // Ascending by Seq. afterSeq: messages newer than it, oldest first.
    // beforeSeq: the latest messages older than it. Neither: the latest messages.
    Task<IReadOnlyList<Message>> GetPage(
        Guid chatId,
        long? afterSeq,
        long? beforeSeq,
        int limit,
        CancellationToken cancellationToken = default);

    Task Add(Message message, CancellationToken cancellationToken = default);
}
