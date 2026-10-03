using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Messenger.Chats;
using Messenger.Infrastructure.Mongo;
using Messenger.Users;
using Messenger.Users.Application;
using MessengerWeb;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

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
});
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddMongo(builder.Configuration);

// One mediator for all modules: the source generator in this project sees every referenced module.
builder.Services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);

builder.Services.AddUsers(builder.Configuration);
builder.Services.AddChats();

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
await app.Services.InitializeChats();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthentication();

app.UseRateLimiter();

app.UseAuthorization();

app.MapControllers();

app.Run();
