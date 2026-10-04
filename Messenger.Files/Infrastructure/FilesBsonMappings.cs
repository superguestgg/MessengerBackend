using Messenger.Files.Domain;
using MongoDB.Bson.Serialization;

namespace Messenger.Files.Infrastructure;

// Keeps persistence details out of the domain types.
internal static class FilesBsonMappings
{
    public static void Register()
    {
        BsonSerializer.TryRegisterSerializer(new FileNameSerializer());
        BsonSerializer.TryRegisterSerializer(new MediaTypeSerializer());

        if (!BsonClassMap.IsClassMapRegistered(typeof(StoredFile)))
            BsonClassMap.RegisterClassMap<StoredFile>(map =>
            {
                map.AutoMap();
                map.GetMemberMap(x => x.AttachedAt).SetIgnoreIfNull(true);
            });
    }
}
