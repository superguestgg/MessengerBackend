using Messenger.Users.Application;
using Messenger.Users.Domain;
using Messenger.Users.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace Messenger.Users;

public static class UsersModule
{
    public static IServiceCollection AddUsers(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<UsersOptions>(
            configuration.GetSection(UsersOptions.SectionName));

        // Driver 3.x refuses to serialize Guid until a representation is chosen.
        BsonSerializer.TryRegisterSerializer(
            new GuidSerializer(GuidRepresentation.Standard));
        BsonSerializer.TryRegisterSerializer(new EmailSerializer());
        BsonSerializer.TryRegisterSerializer(new DisplayNameSerializer());

        services.AddSingleton<IMongoClient>(sp =>
        {
            var options = sp.GetRequiredService<
                    IOptions<UsersOptions>>()
                .Value;

            return new MongoClient(
                options.MongoConnectionString);
        });


        services.AddScoped<IMongoDatabase>(sp =>
        {
            var options = sp.GetRequiredService<
                    IOptions<UsersOptions>>()
                .Value;

            var client = sp.GetRequiredService<IMongoClient>();

            return client.GetDatabase(
                options.DatabaseName);
        });


        services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);


        services.AddScoped<IAccountRepository, AccountRepository>();

        services.AddScoped<IUserProfileRepository, UserProfileRepository>();

        services.AddScoped<IPasswordHasher, PasswordHasher>();


        return services;
    }

    public static async Task InitializeUsers(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();

        var database = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();

        await UsersMongoInitializer.EnsureIndexes(database, cancellationToken);
    }
}