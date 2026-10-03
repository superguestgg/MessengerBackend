using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Messenger.Users.Application;

namespace Messenger.Users.Infrastructure.Authentication;

// Every client sends "Authorization: Bearer <token>". The token is either a JWT
// from login (people in the browser) or an access token (bots, agents); one policy
// scheme looks at the token and forwards to the matching handler.
public static class MessengerAuthentication
{
    public const string Scheme = "Messenger";

    public const string AccessTokenScheme = "AccessToken";

    public static IServiceCollection AddMessengerAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IJwtIssuer, JwtIssuer>();

        services.AddSingleton<IAccessTokenSecrets, AccessTokenSecrets>();


        services.AddAuthentication(Scheme)
            .AddPolicyScheme(Scheme, Scheme, options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    var token = GetBearerToken(context.Request.Headers[HeaderNames.Authorization]);

                    return token != null && token.StartsWith(AccessTokenSecrets.Prefix, StringComparison.Ordinal)
                        ? AccessTokenScheme
                        : JwtBearerDefaults.AuthenticationScheme;
                };
            })
            .AddJwtBearer()
            .AddScheme<AuthenticationSchemeOptions, AccessTokenAuthenticationHandler>(AccessTokenScheme, null);

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((options, jwt) =>
            {
                // Keep "sub" as is instead of mapping it to a long XML claim type.
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Value.Issuer,
                    ValidAudience = jwt.Value.Audience,
                    IssuerSigningKey = JwtIssuer.CreateSigningKey(jwt.Value),
                    NameClaimType = AccountClaims.AccountId,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });


        return services;
    }

    internal static string? GetBearerToken(string? authorizationHeader)
    {
        const string bearer = "Bearer ";

        if (authorizationHeader == null
            || !authorizationHeader.StartsWith(bearer, StringComparison.OrdinalIgnoreCase))
            return null;

        var token = authorizationHeader[bearer.Length..].Trim();

        return token.Length == 0 ? null : token;
    }
}
