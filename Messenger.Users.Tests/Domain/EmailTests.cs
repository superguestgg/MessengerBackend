using Messenger.Users.Domain;

namespace Messenger.Users.Tests.Domain;

public class EmailTests
{
    [Fact]
    public void Email_is_trimmed_and_lowercased()
    {
        var email = new Email("  Foo.Bar@Example.COM ");

        Assert.Equal("foo.bar@example.com", email.Value);
        Assert.Equal("foo.bar@example.com", email.ToString());
    }

    [Fact]
    public void Emails_differing_only_in_case_are_equal()
    {
        Assert.Equal(new Email("Foo@Example.com"), new Email("foo@example.com"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("foo")]
    [InlineData("@example.com")]
    [InlineData("foo@")]
    [InlineData("a@b@c")]
    public void Invalid_email_is_rejected(string value)
    {
        Assert.Throws<DomainException>(() => new Email(value));
        Assert.Null(Email.TryCreate(value));
    }

    [Fact]
    public void Email_up_to_max_length_is_accepted()
    {
        var value = new string('a', Email.MaxLength - "@b".Length) + "@b";

        Assert.Equal(value, new Email(value).Value);
        Assert.Throws<DomainException>(() => new Email("a" + value));
    }

    [Fact]
    public void Length_is_checked_after_trimming()
    {
        var value = new string('a', Email.MaxLength - "@b".Length) + "@b";

        Assert.Equal(value, new Email("  " + value + "  ").Value);
    }

    [Fact]
    public void TryCreate_normalizes_valid_email()
    {
        Assert.Equal("foo@example.com", Email.TryCreate(" Foo@Example.com ")?.Value);
    }
}
