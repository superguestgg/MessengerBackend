using Messenger.Chats.Application;
using Messenger.Chats.Domain;
using Messenger.Chats.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Messenger.Chats;

public static class ChatsModule
{
    // Requires AddMongo(), AddMediator() and an IUsersApi implementation (AddUsers()) from the host.
    public static IServiceCollection AddChats(
        this IServiceCollection services)
    {
        ChatsBsonMappings.Register();


        services.AddScoped<IChatRepository, ChatRepository>();

        services.AddScoped<IMessageRepository, MessageRepository>();

        services.AddScoped<ParticipantLookup>();

        services.AddScoped<ChatResultBuilder>();


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
