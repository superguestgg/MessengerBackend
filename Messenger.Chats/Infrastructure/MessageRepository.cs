using Messenger.Chats.Domain;
using MongoDB.Driver;

namespace Messenger.Chats.Infrastructure;

public sealed class MessageRepository : IMessageRepository
{
    public const string CollectionName = "messages";

    private readonly IMongoCollection<Message> _messages;

    public MessageRepository(IMongoDatabase database)
    {
        _messages = database.GetCollection<Message>(CollectionName);
    }

    public async Task<Message?> GetBySeq(Guid chatId, long seq, CancellationToken cancellationToken = default)
    {
        return await _messages
            .Find(x => x.ChatId == chatId && x.Seq == seq)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Message>> GetPage(
        Guid chatId,
        long? afterSeq,
        long? beforeSeq,
        int limit,
        CancellationToken cancellationToken = default)
    {
        if (afterSeq != null)
            return await _messages
                .Find(x => x.ChatId == chatId && x.Seq > afterSeq.Value)
                .SortBy(x => x.Seq)
                .Limit(limit)
                .ToListAsync(cancellationToken);

        var filter = beforeSeq != null
            ? Builders<Message>.Filter.Where(x => x.ChatId == chatId && x.Seq < beforeSeq.Value)
            : Builders<Message>.Filter.Where(x => x.ChatId == chatId);

        var latest = await _messages
            .Find(filter)
            .SortByDescending(x => x.Seq)
            .Limit(limit)
            .ToListAsync(cancellationToken);

        latest.Reverse();

        return latest;
    }

    public Task Add(Message message, CancellationToken cancellationToken = default)
    {
        return _messages.InsertOneAsync(message, cancellationToken: cancellationToken);
    }
}
