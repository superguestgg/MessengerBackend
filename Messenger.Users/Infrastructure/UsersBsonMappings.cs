using Messenger.Users.Domain;
using MongoDB.Bson.Serialization;

namespace Messenger.Users.Infrastructure;

// Keeps persistence details out of the domain types.
internal static class UsersBsonMappings
{
    public static void Register()
    {
        BsonSerializer.TryRegisterSerializer(new EmailSerializer());
        BsonSerializer.TryRegisterSerializer(new DisplayNameSerializer());

        if (!BsonClassMap.IsClassMapRegistered(typeof(UserProfile)))
            BsonClassMap.RegisterClassMap<UserProfile>(map =>
            {
                map.AutoMap();
                map.MapIdMember(x => x.UserId);
            });
    }
}
