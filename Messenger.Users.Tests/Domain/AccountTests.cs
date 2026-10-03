using Messenger.Users.Domain;

namespace Messenger.Users.Tests.Domain;

public class AccountTests
{
    private static Account RegisterUser()
    {
        return Account.Register(new Email("owner@example.com"), "hash");
    }

    [Fact]
    public void Register_creates_active_user_with_email()
    {
        var account = RegisterUser();

        Assert.Equal(AccountType.User, account.Type);
        Assert.False(account.IsBot);
        Assert.True(account.CanSignInWithPassword);
        Assert.Null(account.OwnerId);
        Assert.IsType<AccountRegistered>(Assert.Single(account.DomainEvents));
    }

    [Fact]
    public void CreateBot_creates_bot_owned_by_user_without_credentials()
    {
        var owner = RegisterUser();

        var bot = owner.CreateBot(new DisplayName(" Helper "));

        Assert.Equal(AccountType.Bot, bot.Type);
        Assert.Equal(owner.Id, bot.OwnerId);
        Assert.Null(bot.Email);
        Assert.Null(bot.PasswordHash);
        Assert.False(bot.CanSignInWithPassword);

        var created = Assert.IsType<BotCreated>(Assert.Single(bot.DomainEvents));
        Assert.Equal(bot.Id, created.BotId);
        Assert.Equal(owner.Id, created.OwnerId);
        Assert.Equal("Helper", created.DisplayName.Value);
    }

    [Fact]
    public void Bot_cannot_create_bots_or_personal_tokens()
    {
        var bot = RegisterUser().CreateBot(new DisplayName("Bot"));

        Assert.Throws<AccessDeniedException>(() => bot.CreateBot(new DisplayName("Child")));
        Assert.Throws<AccessDeniedException>(() => bot.IssuePersonalToken(new AccessTokenName("x"), "hash"));
    }

    [Fact]
    public void IssuePersonalToken_belongs_to_the_user()
    {
        var user = RegisterUser();

        var token = user.IssuePersonalToken(new AccessTokenName(" my agent "), "hash");

        Assert.Equal(user.Id, token.AccountId);
        Assert.Equal("my agent", token.Name.Value);
        Assert.True(token.IsActive);
    }

    [Fact]
    public void IssueBotToken_revokes_previous_tokens()
    {
        var owner = RegisterUser();
        var bot = owner.CreateBot(new DisplayName("Bot"));
        var first = bot.IssueBotToken(owner.Id, [], "hash-1");

        var second = bot.IssueBotToken(owner.Id, [first], "hash-2");

        Assert.False(first.IsActive);
        Assert.True(second.IsActive);
        Assert.Equal(bot.Id, second.AccountId);
    }

    [Fact]
    public void Only_owner_manages_bot_and_stranger_sees_it_as_missing()
    {
        var owner = RegisterUser();
        var bot = owner.CreateBot(new DisplayName("Bot"));
        var stranger = Guid.NewGuid();

        Assert.Throws<AccountNotFoundException>(() => bot.IssueBotToken(stranger, [], "hash"));
        Assert.Throws<AccountNotFoundException>(() => bot.DeleteBot(stranger, []));
    }

    [Fact]
    public void Bot_token_operations_reject_user_accounts()
    {
        var user = RegisterUser();

        Assert.Throws<AccountNotFoundException>(() => user.IssueBotToken(user.Id, [], "hash"));
        Assert.Throws<AccountNotFoundException>(() => user.DeleteBot(user.Id, []));
    }

    [Fact]
    public void DeleteBot_marks_deleted_and_revokes_tokens()
    {
        var owner = RegisterUser();
        var bot = owner.CreateBot(new DisplayName("Bot"));
        var token = bot.IssueBotToken(owner.Id, [], "hash");

        bot.DeleteBot(owner.Id, [token]);

        Assert.Equal(AccountStatus.Deleted, bot.Status);
        Assert.False(bot.IsActive);
        Assert.False(token.IsActive);
        Assert.Throws<AccountNotFoundException>(() => bot.DeleteBot(owner.Id, []));
    }

    [Fact]
    public void Profile_is_editable_by_self_and_by_bot_owner_only()
    {
        var owner = RegisterUser();
        var bot = owner.CreateBot(new DisplayName("Bot"));
        var stranger = Guid.NewGuid();

        owner.EnsureProfileEditableBy(owner.Id);
        bot.EnsureProfileEditableBy(owner.Id);
        bot.EnsureProfileEditableBy(bot.Id);

        Assert.Throws<AccessDeniedException>(() => owner.EnsureProfileEditableBy(stranger));
        Assert.Throws<AccessDeniedException>(() => owner.EnsureProfileEditableBy(bot.Id));
        Assert.Throws<AccessDeniedException>(() => bot.EnsureProfileEditableBy(stranger));
    }

    [Fact]
    public void Token_is_revoked_only_by_its_account()
    {
        var user = RegisterUser();
        var token = user.IssuePersonalToken(new AccessTokenName("agent"), "hash");

        Assert.Throws<AccessTokenNotFoundException>(() => token.Revoke(Guid.NewGuid()));
        Assert.True(token.IsActive);

        token.Revoke(user.Id);

        Assert.False(token.IsActive);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AccessTokenName_rejects_blank(string value)
    {
        Assert.Throws<DomainException>(() => new AccessTokenName(value));
    }

    [Fact]
    public void AccessTokenName_rejects_too_long()
    {
        Assert.Throws<DomainException>(() => new AccessTokenName(new string('a', AccessTokenName.MaxLength + 1)));
    }

    [Fact]
    public void Search_finds_people_but_not_the_requester()
    {
        var user = RegisterUser();
        var other = Account.Register(new Email("other@example.com"), "hash");

        Assert.True(other.IsFoundInSearchBy(user.Id, byExactId: false));
        Assert.False(user.IsFoundInSearchBy(user.Id, byExactId: false));
        Assert.False(user.IsFoundInSearchBy(user.Id, byExactId: true));
    }

    [Fact]
    public void Search_by_name_or_email_hides_someone_elses_bot()
    {
        var owner = RegisterUser();
        var bot = owner.CreateBot(new DisplayName("Bot"));
        var stranger = Guid.NewGuid();

        Assert.True(bot.IsFoundInSearchBy(owner.Id, byExactId: false));
        Assert.False(bot.IsFoundInSearchBy(stranger, byExactId: false));
        Assert.True(bot.IsFoundInSearchBy(stranger, byExactId: true));
    }

    [Fact]
    public void Search_never_finds_a_deleted_bot()
    {
        var owner = RegisterUser();
        var bot = owner.CreateBot(new DisplayName("Bot"));

        bot.DeleteBot(owner.Id, []);

        Assert.False(bot.IsFoundInSearchBy(owner.Id, byExactId: false));
        Assert.False(bot.IsFoundInSearchBy(Guid.NewGuid(), byExactId: true));
    }

    [Theory]
    [InlineData(" Foo@Example.com ", "foo@example.com")]
    [InlineData("a@b", "a@b")]
    public void Email_TryCreate_normalizes_valid_email(string value, string expected)
    {
        Assert.Equal(expected, Email.TryCreate(value)?.Value);
    }

    [Theory]
    [InlineData("foo")]
    [InlineData("@example.com")]
    [InlineData("foo@")]
    [InlineData("a@b@c")]
    public void Email_TryCreate_returns_null_for_invalid_email(string value)
    {
        Assert.Null(Email.TryCreate(value));
    }
}
