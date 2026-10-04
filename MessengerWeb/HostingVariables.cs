namespace MessengerWeb;

// Hosting without Docker hands the app its own variables instead of ours: the address to listen on
// (APP_IP, APP_PORT) and the database it provides (DB_CONNECTION_STRING, DBNAME). When they are set,
// they win over appsettings.json and the Mongo__* variables.
public static class HostingVariables
{
    public static void ApplyHostingVariables(this WebApplicationBuilder builder)
    {
        var configuration = builder.Configuration;

        var port = configuration["APP_PORT"];
        if (!string.IsNullOrEmpty(port))
        {
            // Plain HTTP: the hosting's proxy terminates HTTPS.
            var ip = configuration["APP_IP"];
            builder.WebHost.UseUrls($"http://{(string.IsNullOrEmpty(ip) ? "*" : ip)}:{port}");
        }

        var settings = new Dictionary<string, string?>();

        var connectionString = configuration["DB_CONNECTION_STRING"];
        if (!string.IsNullOrEmpty(connectionString))
            settings["Mongo:ConnectionString"] = connectionString;

        var databaseName = configuration["DBNAME"];
        if (!string.IsNullOrEmpty(databaseName))
            settings["Mongo:DatabaseName"] = databaseName;

        if (settings.Count > 0)
            configuration.AddInMemoryCollection(settings);
    }
}
