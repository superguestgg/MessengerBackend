using Messenger.Chats.Domain;
using MongoDB.Driver;

namespace Messenger.Chats.Infrastructure;

public static class ChatsMongoInitializer
{
    public static async Task EnsureIndexes(
        IMongoDatabase database,
        CancellationToken cancellationToken = default)
    {
        var chats = database.GetCollection<Chat>(ChatRepository.CollectionName);

        var directIndex = new CreateIndexModel<Chat>(
            Builders<Chat>.IndexKeys.Ascending(x => x.DirectKey),
            new CreateIndexOptions<Chat>
            {
                Unique = true,
                Name = "ux_chats_direct_key",
                PartialFilterExpression = Builders<Chat>.Filter.Exists(x => x.DirectKey)
            });

        var memberIndex = new CreateIndexModel<Chat>(
            Builders<Chat>.IndexKeys.Ascending($"{ChatsBsonMappings.MembersElement}.UserId"),
            new CreateIndexOptions { Name = "ix_chats_member" });

        await chats.Indexes.CreateManyAsync(
            [directIndex, memberIndex],
            cancellationToken);


        var messages = database.GetCollection<Message>(MessageRepository.CollectionName);

        var seqIndex = new CreateIndexModel<Message>(
            Builders<Message>.IndexKeys
                .Ascending(x => x.ChatId)
                .Ascending(x => x.Seq),
            new CreateIndexOptions { Unique = true, Name = "ux_messages_chat_seq" });

        await messages.Indexes.CreateOneAsync(
            seqIndex,
            cancellationToken: cancellationToken);
    }
}
