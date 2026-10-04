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

        // Driver 2.x because the hosting runs MongoDB 3.6, which 3.x does not support. V3 mode is what
        // 3.x always does: no global legacy Guid format, Guids are stored as standard UUIDs (subtype 4).
#pragma warning disable CS0618
        BsonDefaults.GuidRepresentationMode = GuidRepresentationMode.V3;
#pragma warning restore CS0618

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
