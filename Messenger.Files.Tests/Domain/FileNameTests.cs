using Messenger.Files.Domain;

namespace Messenger.Files.Tests.Domain;

public class FileNameTests
{
    [Theory]
    [InlineData("report.pdf", "report.pdf")]
    [InlineData("  report.pdf ", "report.pdf")]
    [InlineData("C:\\Users\\me\\report.pdf", "report.pdf")]
    [InlineData("../../etc/passwd", "passwd")]
    [InlineData("a\nb.txt", "a_b.txt")]
    public void Name_is_reduced_to_a_safe_last_segment(string value, string expected)
    {
        Assert.Equal(expected, new FileName(value).Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("folder/")]
    public void Empty_name_is_rejected(string value)
    {
        Assert.Throws<DomainException>(() => new FileName(value));
    }

    [Fact]
    public void Name_length_is_limited()
    {
        var max = new string('a', FileName.MaxLength);

        Assert.Equal(max, new FileName(max).Value);
        Assert.Throws<DomainException>(() => new FileName(max + "a"));
    }
}
