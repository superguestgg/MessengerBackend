using System.Security.Cryptography;
using System.Text;
using Messenger.Users.Application;
using Microsoft.AspNetCore.WebUtilities;

namespace Messenger.Users.Infrastructure.Authentication;

public sealed class AccessTokenSecrets : IAccessTokenSecrets
{
    // A visible prefix tells access tokens from JWTs and makes leaked tokens easy to search for.
    public const string Prefix = "msg_";

    private const int SecretBytes = 32;

    public (string Token, string TokenHash) Generate()
    {
        var token = Prefix + WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(SecretBytes));

        return (token, Hash(token));
    }

    public bool LooksLikeToken(string value)
    {
        return value.StartsWith(Prefix, StringComparison.Ordinal);
    }

    // The secret is 256 random bits, so a fast hash is enough: there is nothing to brute-force.
    public string Hash(string token)
    {
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
