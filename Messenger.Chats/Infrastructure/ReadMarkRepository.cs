using Messenger.Chats.Domain;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Messenger.Chats.Infrastructure;

public sealed class ReadMarkRepository : IReadMarkRepository
{
    public const string CollectionName = "chat_read_marks";

    public const string ChatIdElement = "ChatId";

    public const string UserIdElement = "UserId";

    public const string LastReadSeqElement = "LastReadSeq";

    private readonly IMongoCollection<BsonDocument> _marks;

    public ReadMarkRepository(IMongoDatabase database)
    {
        _marks = database.GetCollection<BsonDocument>(CollectionName);
    }

    public Task Advance(ReadMark mark, CancellationToken cancellationToken = default)
    {
        // The filter matches the unique index exactly, so the server retries a concurrent
        // upsert that loses the race instead of failing it with a duplicate key.
        return _marks.UpdateOneAsync(
            new BsonDocument
            {
                { UserIdElement, ToBson(mark.UserId) },
                { ChatIdElement, ToBson(mark.ChatId) }
            },
            new BsonDocument("$max", new BsonDocument(LastReadSeqElement, mark.Seq)),
            new UpdateOptions { IsUpsert = true },
            cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, long>> GetForMember(
        Guid userId,
        IReadOnlyCollection<Guid> chatIds,
        CancellationToken cancellationToken = default)
    {
        if (chatIds.Count == 0)
            return new Dictionary<Guid, long>();

        var marks = await _marks
            .Find(new BsonDocument
            {
                { UserIdElement, ToBson(userId) },
                { ChatIdElement, new BsonDocument("$in", new BsonArray(chatIds.Select(ToBson))) }
            })
            .ToListAsync(cancellationToken);

        return marks.ToDictionary(
            x => x[ChatIdElement].AsGuid,
            x => x[LastReadSeqElement].AsInt64);
    }

    private static BsonBinaryData ToBson(Guid id)
    {
        return new BsonBinaryData(id, GuidRepresentation.Standard);
    }
}
