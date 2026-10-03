namespace Messenger.Infrastructure.Mongo;

public sealed class MongoOptions
{
    public const string SectionName = "Mongo";

    public string ConnectionString { get; set; } = null!;

    public string DatabaseName { get; set; } = null!;
}
