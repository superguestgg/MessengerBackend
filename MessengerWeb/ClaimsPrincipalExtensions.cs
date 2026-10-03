using System.Security.Claims;
using Messenger.Users.Application;

namespace MessengerWeb;

public static class ClaimsPrincipalExtensions
{
    // Only called behind [Authorize], so a missing claim is a bug, not a client error.
    public static Guid GetAccountId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(AccountClaims.AccountId);

        if (value == null || !Guid.TryParse(value, out var accountId))
            throw new InvalidOperationException("Authenticated principal has no account id.");

        return accountId;
    }
}
