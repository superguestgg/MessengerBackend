namespace MessengerWeb;

// One-domain deployment: the frontend build is copied to wwwroot (npm run build:host) and served
// from here, so no CORS is needed. Without wwwroot nothing is served and the frontend lives elsewhere.
public static class FrontendHosting
{
    // Vite puts a content hash into asset names, so they are cached for good; index.html is not.
    public static readonly StaticFileOptions StaticFileOptions = new()
    {
        OnPrepareResponse = context =>
        {
            context.Context.Response.Headers.CacheControl =
                context.Context.Request.Path.StartsWithSegments("/assets")
                    ? "public, max-age=31536000, immutable"
                    : "no-cache";
        }
    };

    public static void MapFrontendFallback(this WebApplication app)
    {
        // An unknown API or MCP path is a 404, not the frontend's index.html.
        app.MapFallback("/api/{**path}", () => Results.NotFound());
        app.MapFallback("/mcp/{**path}", () => Results.NotFound());

        // Client-side routes (/chats/…) get index.html.
        app.MapFallbackToFile("index.html", StaticFileOptions)
            .AllowAnonymous();

        // A missing file (/robots.txt, an old asset) is a 404 for everyone, not a 401.
        app.MapFallback("{*path:file}", () => Results.NotFound())
            .AllowAnonymous();
    }
}
