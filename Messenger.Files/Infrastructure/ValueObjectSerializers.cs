using Messenger.Files.Domain;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Messenger.Files.Infrastructure;

// Value objects are stored as plain strings.

public sealed class FileNameSerializer : SerializerBase<FileName>
{
    public override FileName Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        return new FileName(context.Reader.ReadString());
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, FileName value)
    {
        context.Writer.WriteString(value.Value);
    }
}

public sealed class MediaTypeSerializer : SerializerBase<MediaType>
{
    public override MediaType Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        return new MediaType(context.Reader.ReadString());
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, MediaType value)
    {
        context.Writer.WriteString(value.Value);
    }
}
