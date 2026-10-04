using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Messenger.Chats;
using Messenger.Files;
using Messenger.Infrastructure.Mongo;
using Messenger.Users;
using Messenger.Users.Application;
using MessengerWeb;
using MessengerWeb.Mcp;
using ModelContextProtocol.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    // The hosting may start the published app from another directory: appsettings.json and wwwroot
    // are looked up next to the executable, not in the current directory.
    ContentRootPath = AppContext.BaseDirectory
});

builder.ApplyHostingVariables();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        Description = "JWT from /api/auth/login or an access token (msg_...)"
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });

    // Precise nullability and "required" for the frontend's generated client (npm run api:generate).
    options.SupportNonNullableReferenceTypes();
    options.UseAllOfToExtendReferenceSchemas();
    options.SchemaFilter<RequireNonNullablePropertiesFilter>();
});
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();

// Behind a reverse proxy on the same machine the client's IP (for rate limiting) and scheme come in
// X-Forwarded-* headers. Only loopback proxies are trusted, as by default.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);

builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddMongo(builder.Configuration);

// One mediator for all modules: the source generator in this project sees every referenced module.
builder.Services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);

builder.Services.AddUsers(builder.Configuration);
builder.Services.AddFiles();
builder.Services.AddChats();

// MCP for agents at /mcp: stateless Streamable HTTP, the same bearer tokens as the REST API.
builder.Services.AddMcpServer()
    .WithHttpTransport(options => options.SessionMode = HttpServerSessionMode.Stateless)
    .WithTools<MessengerTools>(MessengerTools.SerializerOptions);

// Every endpoint needs a signed-in account unless it is marked [AllowAnonymous].
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());

// The frontend may live on another domain. Clients send the token in a header,
// not a cookie, so credentials are not allowed.
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .SetPreflightMaxAge(TimeSpan.FromMinutes(10))));

// Per account when signed in, per IP otherwise (registration and login get the stricter limit).
var rateLimits = builder.Configuration.GetSection("RateLimiting");
var accountPermits = rateLimits.GetValue("AccountPermitsPerMinute", 600);
var anonymousPermits = rateLimits.GetValue("AnonymousPermitsPerMinute", 60);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var accountId = context.User.FindFirstValue(AccountClaims.AccountId);

        var (key, permits) = accountId != null
            ? ("account:" + accountId, accountPermits)
            : ("ip:" + context.Connection.RemoteIpAddress, anonymousPermits);

        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permits,
            Window = TimeSpan.FromMinutes(1)
        });
    });
});
var app = builder.Build();

await app.Services.InitializeUsers();
await app.Services.InitializeFiles();
await app.Services.InitializeChats();

app.UseForwardedHeaders();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// The built frontend, if it is in wwwroot. Static files are public, so this goes before authentication,
// and before routing: otherwise the fallback endpoints would take requests for existing files.
app.UseStaticFiles(FrontendHosting.StaticFileOptions);

app.UseRouting();

app.UseCors();

app.UseAuthentication();

app.UseRateLimiter();

app.UseAuthorization();

app.MapControllers();

// Covered by the fallback policy: only signed-in accounts reach the tools.
app.MapMcp("/mcp");

app.MapFrontendFallback();

app.Run();
