using Messenger.Files.Domain;

namespace Messenger.Files.Tests.Domain;

public class MediaTypeTests
{
    [Theory]
    [InlineData("image/png", "image/png")]
    [InlineData("Image/PNG", "image/png")]
    [InlineData("audio/webm;codecs=opus", "audio/webm")]
    [InlineData(" text/plain ; charset=utf-8", "text/plain")]
    [InlineData("application/vnd.openxmlformats-officedocument.wordprocessingml.document", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    public void Type_is_normalized(string value, string expected)
    {
        Assert.Equal(expected, new MediaType(value).Value);
    }

    [Theory]
    [InlineData("png")]
    [InlineData("image/")]
    [InlineData("image/png/x")]
    [InlineData("text/html<script>")]
    public void Malformed_type_is_rejected(string value)
    {
        Assert.Throws<DomainException>(() => new MediaType(value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Missing_type_is_unknown(string? value)
    {
        Assert.Equal("application/octet-stream", MediaType.OrUnknown(value).Value);
    }
}
