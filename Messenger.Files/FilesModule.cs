using Messenger.Files.Application;
using Messenger.Files.Contracts;
using Messenger.Files.Domain;
using Messenger.Files.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;

namespace Messenger.Files;

public static class FilesModule
{
    // Requires AddMongo() and AddMediator() from the host.
    public static IServiceCollection AddFiles(
        this IServiceCollection services)
    {
        FilesBsonMappings.Register();


        services.AddScoped<IStoredFileRepository, StoredFileRepository>();

        services.AddScoped<IFileStorage, GridFsFileStorage>();

        services.AddScoped<IFilesApi, FilesApi>();


        services.AddHostedService<AbandonedFilesCleanup>();

        services.TryAddSingleton(TimeProvider.System);


        return services;
    }

    public static async Task InitializeFiles(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();

        var database = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();

        await FilesMongoInitializer.EnsureIndexes(database, cancellationToken);
    }
}
