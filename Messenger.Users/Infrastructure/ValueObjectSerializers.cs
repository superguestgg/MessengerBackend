using Messenger.Users.Domain;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Messenger.Users.Infrastructure;

// Value objects are stored as plain strings, so documents, indexes and
// queries keep the same shape they had before the value objects existed.

public sealed class EmailSerializer : SerializerBase<Email>
{
    public override Email Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        return new Email(context.Reader.ReadString());
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Email value)
    {
        context.Writer.WriteString(value.Value);
    }
}

public sealed class DisplayNameSerializer : SerializerBase<DisplayName>
{
    public override DisplayName Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        return new DisplayName(context.Reader.ReadString());
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, DisplayName value)
    {
        context.Writer.WriteString(value.Value);
    }
}
