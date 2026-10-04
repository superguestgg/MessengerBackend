using Messenger.Chats.Domain;
using MongoDB.Bson.Serialization;

namespace Messenger.Chats.Infrastructure;

// Keeps persistence details out of the domain types.
internal static class ChatsBsonMappings
{
    public const string MembersElement = "Members";

    public const string AttachmentsElement = "Attachments";

    public static void Register()
    {
        BsonSerializer.TryRegisterSerializer(new ChatTitleSerializer());
        BsonSerializer.TryRegisterSerializer(new MessageTextSerializer());

        if (!BsonClassMap.IsClassMapRegistered(typeof(Chat)))
            BsonClassMap.RegisterClassMap<Chat>(map =>
            {
                map.AutoMap();

                // Members is a read-only view; the list behind it is what gets stored.
                map.MapField("_members").SetElementName(MembersElement);

                map.GetMemberMap(x => x.Title).SetIgnoreIfNull(true);
                map.GetMemberMap(x => x.DirectKey).SetIgnoreIfNull(true);
            });

        if (!BsonClassMap.IsClassMapRegistered(typeof(ChatMember)))
            BsonClassMap.RegisterClassMap<ChatMember>(map =>
            {
                map.AutoMap();
            });

        if (!BsonClassMap.IsClassMapRegistered(typeof(Message)))
            BsonClassMap.RegisterClassMap<Message>(map =>
            {
                map.AutoMap();
                map.GetMemberMap(x => x.ReplyToSeq).SetIgnoreIfNull(true);
                map.GetMemberMap(x => x.Text).SetIgnoreIfNull(true);

                // Attachments is a read-only view; the list behind it is what gets stored.
                // Plain text messages keep their old shape: no empty array.
                map.MapField("_attachments")
                    .SetElementName(AttachmentsElement)
                    .SetShouldSerializeMethod(x => ((Message)x).Attachments.Count > 0);
            });

        if (!BsonClassMap.IsClassMapRegistered(typeof(Attachment)))
            BsonClassMap.RegisterClassMap<Attachment>(map =>
            {
                map.AutoMap();
                map.GetMemberMap(x => x.DurationSeconds).SetIgnoreIfNull(true);
                map.GetMemberMap(x => x.Transcript).SetIgnoreIfNull(true);
            });

        if (!BsonClassMap.IsClassMapRegistered(typeof(Transcript)))
            BsonClassMap.RegisterClassMap<Transcript>(map =>
            {
                map.AutoMap();
                map.GetMemberMap(x => x.Text).SetIgnoreIfNull(true);
            });
    }
}
