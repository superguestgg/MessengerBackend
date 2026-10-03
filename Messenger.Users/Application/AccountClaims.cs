namespace Messenger.Users.Application;

// Claims put on the authenticated principal, whichever way the account signed in.
public static class AccountClaims
{
    public const string AccountId = "sub";

    public const string AccountType = "account_type";
}
