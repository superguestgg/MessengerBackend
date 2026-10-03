using Messenger.Users.Domain;

namespace Messenger.Users.Application;

public interface IJwtIssuer
{
    (string Token, DateTime ExpiresAt) Issue(Account account);
}
