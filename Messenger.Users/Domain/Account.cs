namespace Messenger.Users.Domain;

// A user and a bot are the same kind of account: chats and messages work
// the same for both. They differ only in how they sign in and who manages them.
public class Account : AggregateRoot
{
    private Account()
    {
    }


    public static Account Register(
        Email email,
        string passwordHash)
    {
        var now = DateTime.UtcNow;

        var account = new Account
        {
            Id = Guid.NewGuid(),
            Type = AccountType.User,
            Email = email,
            PasswordHash = passwordHash,
            Status = AccountStatus.Active,
            CreatedAt = now,
            UpdatedAt = now
        };

        account.Raise(new AccountRegistered(account.Id, email));

        return account;
    }

    public Account CreateBot(DisplayName displayName)
    {
        EnsureActiveUser("create bots");

        var now = DateTime.UtcNow;

        var bot = new Account
        {
            Id = Guid.NewGuid(),
            Type = AccountType.Bot,
            OwnerId = Id,
            Status = AccountStatus.Active,
            CreatedAt = now,
            UpdatedAt = now
        };

        bot.Raise(new BotCreated(bot.Id, Id, displayName));

        return bot;
    }

    public AccessToken IssuePersonalToken(
        AccessTokenName name,
        string tokenHash)
    {
        EnsureActiveUser("issue access tokens");

        return AccessToken.Issue(Id, name, tokenHash);
    }

    // A bot has a single valid token: issuing a new one revokes the previous ones.
    public AccessToken IssueBotToken(
        Guid requesterId,
        IEnumerable<AccessToken> activeTokens,
        string tokenHash)
    {
        EnsureBotOwnedBy(requesterId);

        RevokeAll(activeTokens);

        return AccessToken.Issue(Id, AccessToken.BotTokenName, tokenHash);
    }

    public void DeleteBot(
        Guid requesterId,
        IEnumerable<AccessToken> activeTokens)
    {
        EnsureBotOwnedBy(requesterId);

        RevokeAll(activeTokens);

        Status = AccountStatus.Deleted;
        UpdatedAt = DateTime.UtcNow;
    }

    public void EnsureProfileEditableBy(Guid requesterId)
    {
        if (Status == AccountStatus.Deleted)
            throw new AccountNotFoundException(Id);

        if (requesterId == Id)
            return;

        if (Type == AccountType.Bot && OwnerId == requesterId)
            return;

        throw new AccessDeniedException("Only the account itself or the bot owner can edit this profile.");
    }

    public bool CanSignInWithPassword =>
        Type == AccountType.User && IsActive && PasswordHash != null;

    public bool IsActive => Status == AccountStatus.Active;

    public bool IsBot => Type == AccountType.Bot;

    private void EnsureActiveUser(string action)
    {
        if (Type != AccountType.User)
            throw new AccessDeniedException($"Bots cannot {action}.");

        if (!IsActive)
            throw new AccessDeniedException($"Inactive accounts cannot {action}.");
    }

    // Someone else's bot is reported as missing, so bot ids cannot be probed.
    private void EnsureBotOwnedBy(Guid requesterId)
    {
        if (Type != AccountType.Bot || OwnerId != requesterId || Status == AccountStatus.Deleted)
            throw new AccountNotFoundException(Id);
    }

    private void RevokeAll(IEnumerable<AccessToken> tokens)
    {
        foreach (var token in tokens)
        {
            if (token.AccountId != Id)
                throw new InvalidOperationException("Token belongs to another account.");

            token.RevokeInternal();
        }
    }

    public Guid Id { get; private set; }

    public AccountType Type { get; private set; }

    // Only users sign in with a password; bots have neither email nor password.
    public Email? Email { get; private set; }

    public string? PasswordHash { get; private set; }

    public Guid? OwnerId { get; private set; }

    public AccountStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }
}
