using Messenger.Users.Domain;
using Messenger.Users.Ifrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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


        services.AddScoped<IUserRepository, UserRepository>();

        services.AddScoped<IUserProfileRepository, UserProfileRepository>();

        services.AddScoped<IPasswordHasher, PasswordHasher>();


        return services;
    }

}