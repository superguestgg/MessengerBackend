using Messenger.Chats.Domain;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Messenger.Chats.Infrastructure;

// Value objects are stored as plain strings.

public sealed class ChatTitleSerializer : SerializerBase<ChatTitle>
{
    public override ChatTitle Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        return new ChatTitle(context.Reader.ReadString());
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, ChatTitle value)
    {
        context.Writer.WriteString(value.Value);
    }
}

public sealed class MessageTextSerializer : SerializerBase<MessageText>
{
    public override MessageText Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        return new MessageText(context.Reader.ReadString());
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, MessageText value)
    {
        context.Writer.WriteString(value.Value);
    }
}
