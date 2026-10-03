using Messenger.Users.Domain;

namespace Messenger.Users.Tests.Domain;

public class DisplayNameTests
{
    [Fact]
    public void Name_is_trimmed_but_keeps_case_and_inner_spaces()
    {
        var name = new DisplayName("  Иван  Петров ");

        Assert.Equal("Иван  Петров", name.Value);
        Assert.Equal("Иван  Петров", name.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void Blank_name_is_rejected(string value)
    {
        Assert.Throws<DomainException>(() => new DisplayName(value));
    }

    [Fact]
    public void Name_up_to_max_length_is_accepted()
    {
        var value = new string('a', DisplayName.MaxLength);

        Assert.Equal(value, new DisplayName(value).Value);
        Assert.Equal(value, new DisplayName(" " + value + " ").Value);
        Assert.Throws<DomainException>(() => new DisplayName(value + "a"));
    }

    [Fact]
    public void Names_are_compared_by_value()
    {
        Assert.Equal(new DisplayName("Bob"), new DisplayName(" Bob "));
        Assert.NotEqual(new DisplayName("Bob"), new DisplayName("bob"));
    }
}
