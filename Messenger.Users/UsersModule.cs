using Messenger.Users.Application;
using Messenger.Users.Contracts;
using Messenger.Users.Domain;
using Messenger.Users.Infrastructure;
using Messenger.Users.Infrastructure.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Messenger.Users;

public static class UsersModule
{
    // Requires AddMongo() from Messenger.Infrastructure.Mongo and AddMediator() to be called by the host.
    public static IServiceCollection AddUsers(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        UsersBsonMappings.Register();


        services.AddScoped<IAccountRepository, AccountRepository>();

        services.AddScoped<IUserProfileRepository, UserProfileRepository>();

        services.AddScoped<IAccessTokenRepository, AccessTokenRepository>();

        services.AddScoped<IPasswordHasher, PasswordHasher>();

        services.AddScoped<IUsersApi, UsersApi>();


        services.AddMessengerAuthentication(configuration);


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
