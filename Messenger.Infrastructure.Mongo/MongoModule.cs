using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace Messenger.Infrastructure.Mongo;

public static class MongoModule
{
    // Shared by all modules: one client, one database, collections owned per module.
    public static IServiceCollection AddMongo(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<MongoOptions>(
            configuration.GetSection(MongoOptions.SectionName));

        // Driver 3.x refuses to serialize Guid until a representation is chosen.
        BsonSerializer.TryRegisterSerializer(
            new GuidSerializer(GuidRepresentation.Standard));


        services.TryAddSingleton<IMongoClient>(sp =>
        {
            var options = sp.GetRequiredService<
                    IOptions<MongoOptions>>()
                .Value;

            return new MongoClient(
                options.ConnectionString);
        });


        services.TryAddScoped<IMongoDatabase>(sp =>
        {
            var options = sp.GetRequiredService<
                    IOptions<MongoOptions>>()
                .Value;

            var client = sp.GetRequiredService<IMongoClient>();

            return client.GetDatabase(
                options.DatabaseName);
        });


        return services;
    }
}
