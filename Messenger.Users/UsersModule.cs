using Messenger.Users.Application;
using Messenger.Users.Domain;
using Messenger.Users.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Messenger.Users;

public static class UsersModule
{
    // Requires AddMongo() from Messenger.Infrastructure.Mongo to be called by the host.
    public static IServiceCollection AddUsers(
        this IServiceCollection services)
    {
        UsersBsonMappings.Register();


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
