using Messenger.Chats.Domain;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Messenger.Chats.Infrastructure;

public sealed class ChatRepository : IChatRepository
{
    public const string CollectionName = "chats";

    // Kept apart from chat documents: sending a message only bumps a counter here
    // and never races with member changes that replace the chat document.
    public const string CountersCollectionName = "chat_message_counters";

    private readonly IMongoCollection<Chat> _chats;
    private readonly IMongoCollection<BsonDocument> _counters;

    public ChatRepository(IMongoDatabase database)
    {
        _chats = database.GetCollection<Chat>(CollectionName);
        _counters = database.GetCollection<BsonDocument>(CountersCollectionName);
    }

    public async Task<Chat?> Get(Guid id, CancellationToken cancellationToken = default)
    {
        return await _chats
            .Find(x => x.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Chat?> GetDirect(string directKey, CancellationToken cancellationToken = default)
    {
        return await _chats
            .Find(x => x.DirectKey == directKey)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Chat>> GetByMember(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _chats
            .Find(MemberFilter(userId))
            .ToListAsync(cancellationToken);
    }

    public async Task Add(Chat chat, CancellationToken cancellationToken = default)
    {
        try
        {
            await _chats.InsertOneAsync(chat, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException e)
            when (e.WriteError.Category == ServerErrorCategory.DuplicateKey && chat.DirectKey != null)
        {
            throw new DirectChatAlreadyExistsException(chat.DirectKey);
        }
    }

    public async Task Update(Chat chat, CancellationToken cancellationToken = default)
    {
        var expectedVersion = chat.Version;

        chat.IncrementVersion();

        var result = await _chats.ReplaceOneAsync(
            x => x.Id == chat.Id && x.Version == expectedVersion,
            chat,
            cancellationToken: cancellationToken);

        if (result.MatchedCount == 0)
            throw new ChatConcurrencyException(chat.Id);
    }

    public async Task<long> NextMessageSeq(Guid chatId, DateTime sentAt, CancellationToken cancellationToken = default)
    {
        var counter = await _counters.FindOneAndUpdateAsync(
            new BsonDocument("_id", new BsonBinaryData(chatId, GuidRepresentation.Standard)),
            new BsonDocument
            {
                { "$inc", new BsonDocument("LastSeq", 1L) },
                { "$set", new BsonDocument("LastMessageAt", sentAt) }
            },
            new FindOneAndUpdateOptions<BsonDocument>
            {
                IsUpsert = true,
                ReturnDocument = ReturnDocument.After
            },
            cancellationToken);

        return counter["LastSeq"].AsInt64;
    }

    public async Task<IReadOnlyDictionary<Guid, ChatActivity>> GetActivity(
        IReadOnlyCollection<Guid> chatIds,
        CancellationToken cancellationToken = default)
    {
        if (chatIds.Count == 0)
            return new Dictionary<Guid, ChatActivity>();

        var ids = new BsonArray(chatIds.Select(x => new BsonBinaryData(x, GuidRepresentation.Standard)));

        var counters = await _counters
            .Find(new BsonDocument("_id", new BsonDocument("$in", ids)))
            .ToListAsync(cancellationToken);

        return counters.ToDictionary(
            x => x["_id"].AsGuid,
            x => new ChatActivity(
                x["LastSeq"].AsInt64,
                x["LastMessageAt"].ToUniversalTime()));
    }

    internal static FilterDefinition<Chat> MemberFilter(Guid userId)
    {
        return Builders<Chat>.Filter.ElemMatch<ChatMember>(
            ChatsBsonMappings.MembersElement,
            Builders<ChatMember>.Filter.Eq(x => x.UserId, userId));
    }
}
