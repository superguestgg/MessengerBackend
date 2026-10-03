namespace Messenger.Users.Application;

public interface IAccessTokenSecrets
{
    // Returns the secret to show once and the hash to store.
    (string Token, string TokenHash) Generate();

    // False for anything that cannot be one of our tokens, so JWTs are not looked up.
    bool LooksLikeToken(string value);

    string Hash(string token);
}
