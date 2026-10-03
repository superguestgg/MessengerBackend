using System.Security.Claims;
using System.Text;
using Messenger.Users.Application;
using Messenger.Users.Domain;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Messenger.Users.Infrastructure.Authentication;

public sealed class JwtIssuer : IJwtIssuer
{
    private readonly JwtOptions _options;
    private readonly JsonWebTokenHandler _handler = new();

    public JwtIssuer(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public (string Token, DateTime ExpiresAt) Issue(Account account)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_options.LifetimeMinutes);

        var token = _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Expires = expiresAt,
            Subject = new ClaimsIdentity(
            [
                new Claim(AccountClaims.AccountId, account.Id.ToString()),
                new Claim(AccountClaims.AccountType, account.Type.ToString())
            ]),
            SigningCredentials = new SigningCredentials(
                CreateSigningKey(_options),
                SecurityAlgorithms.HmacSha256)
        });

        return (token, expiresAt);
    }

    public static SymmetricSecurityKey CreateSigningKey(JwtOptions options)
    {
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey));
    }
}
