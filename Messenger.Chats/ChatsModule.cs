using Messenger.Chats.Application;
using Messenger.Chats.Domain;
using Messenger.Chats.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;

namespace Messenger.Chats;

public static class ChatsModule
{
    // Requires AddMongo(), AddMediator() and implementations of IUsersApi (AddUsers()) and IFilesApi (AddFiles()) from the host.
    public static IServiceCollection AddChats(
        this IServiceCollection services)
    {
        ChatsBsonMappings.Register();


        services.AddScoped<IChatRepository, ChatRepository>();

        services.AddScoped<IMessageRepository, MessageRepository>();

        services.AddScoped<IReadMarkRepository, ReadMarkRepository>();

        services.AddScoped<ParticipantLookup>();

        services.AddScoped<ChatResultBuilder>();

        services.AddScoped<MessageResultBuilder>();


        // Process-wide: waits and the messages that wake them meet here.
        services.AddSingleton<IChatSignals, InMemoryChatSignals>();

        services.AddSingleton<WaitSlots>();

        services.TryAddSingleton(TimeProvider.System);


        return services;
    }

    public static async Task InitializeChats(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();

        var database = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();

        await ChatsMongoInitializer.EnsureIndexes(database, cancellationToken);
    }
}
