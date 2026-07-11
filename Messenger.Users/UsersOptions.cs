namespace Messenger.Users;

public sealed class UsersOptions
{
    public const string SectionName = "Users";

    public string MongoConnectionString { get; set; } = null!;

    public string DatabaseName { get; set; } = null!;
}