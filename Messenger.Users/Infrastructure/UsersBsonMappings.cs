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
        BsonSerializer.TryRegisterSerializer(new AccessTokenNameSerializer());

        if (!BsonClassMap.IsClassMapRegistered(typeof(Account)))
            BsonClassMap.RegisterClassMap<Account>(map =>
            {
                map.AutoMap();

                // Accounts stored before bots existed have no Type field.
                map.GetMemberMap(x => x.Type).SetDefaultValue(AccountType.User);

                // Bots have no email: the field is left out, so the partial unique index skips them.
                map.GetMemberMap(x => x.Email).SetIgnoreIfNull(true);
                map.GetMemberMap(x => x.PasswordHash).SetIgnoreIfNull(true);
                map.GetMemberMap(x => x.OwnerId).SetIgnoreIfNull(true);
            });

        if (!BsonClassMap.IsClassMapRegistered(typeof(UserProfile)))
            BsonClassMap.RegisterClassMap<UserProfile>(map =>
            {
                map.AutoMap();
                map.MapIdMember(x => x.UserId);
            });

        if (!BsonClassMap.IsClassMapRegistered(typeof(AccessToken)))
            BsonClassMap.RegisterClassMap<AccessToken>(map =>
            {
                map.AutoMap();
            });
    }
}
