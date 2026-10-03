using Messenger.Users.Infrastructure.Authentication;

namespace Messenger.Users.Tests.Infrastructure;

public class AccessTokenSecretsTests
{
    private readonly AccessTokenSecrets _secrets = new();

    [Fact]
    public void Generate_returns_prefixed_unique_tokens_with_matching_hash()
    {
        var (first, firstHash) = _secrets.Generate();
        var (second, _) = _secrets.Generate();

        Assert.StartsWith(AccessTokenSecrets.Prefix, first);
        Assert.NotEqual(first, second);
        Assert.Equal(firstHash, _secrets.Hash(first));
        Assert.DoesNotContain(first, firstHash);
    }

    [Theory]
    [InlineData("msg_abc", true)]
    [InlineData("eyJhbGciOi.payload.sig", false)]
    [InlineData("MSG_abc", false)]
    public void LooksLikeToken_checks_prefix(string value, bool expected)
    {
        Assert.Equal(expected, _secrets.LooksLikeToken(value));
    }
}
