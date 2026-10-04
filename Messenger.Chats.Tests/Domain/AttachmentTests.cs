using Messenger.Chats.Domain;

namespace Messenger.Chats.Tests.Domain;

public class AttachmentTests
{
    private static Attachment File(string contentType = "application/pdf") =>
        Attachment.File(Guid.NewGuid(), "report.pdf", contentType, 1000);

    private static Attachment Voice(int durationSeconds = 12) =>
        Attachment.Voice(Guid.NewGuid(), "voice.webm", "audio/webm", 1000, durationSeconds);

    [Theory]
    [InlineData("image/png", AttachmentKind.Image)]
    [InlineData("image/jpeg", AttachmentKind.Image)]
    [InlineData("image/webp", AttachmentKind.Image)]
    [InlineData("image/svg+xml", AttachmentKind.File)]
    [InlineData("text/html", AttachmentKind.File)]
    [InlineData("audio/webm", AttachmentKind.File)]
    public void File_kind_follows_the_content_type(string contentType, AttachmentKind kind)
    {
        Assert.Equal(kind, File(contentType).Kind);
    }

    [Fact]
    public void Only_images_and_voice_are_shown_inline()
    {
        Assert.True(File("image/png").CanBeShownInline);
        Assert.True(Voice().CanBeShownInline);
        Assert.False(File("text/html").CanBeShownInline);
        Assert.False(File("image/svg+xml").CanBeShownInline);
    }

    [Fact]
    public void Voice_starts_without_a_transcript()
    {
        var voice = Voice(42);

        Assert.Equal(AttachmentKind.Voice, voice.Kind);
        Assert.Equal(42, voice.DurationSeconds);
        Assert.Equal(TranscriptStatus.None, voice.Transcript?.Status);
        Assert.Null(voice.Transcript?.Text);
        Assert.Null(File().Transcript);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(Attachment.MaxVoiceDurationSeconds + 1)]
    public void Voice_duration_is_limited(int durationSeconds)
    {
        Assert.Throws<DomainException>(() => Voice(durationSeconds));
        Assert.Equal(Attachment.MaxVoiceDurationSeconds, Voice(Attachment.MaxVoiceDurationSeconds).DurationSeconds);
    }

    [Fact]
    public void Voice_must_be_audio()
    {
        Assert.Throws<DomainException>(() =>
            Attachment.Voice(Guid.NewGuid(), "voice.webm", "video/webm", 1000, 5));
    }

    [Fact]
    public void Content_needs_text_or_attachments()
    {
        Assert.Throws<DomainException>(() => new MessageContent(null, []));

        Assert.Single(new MessageContent(null, [File()]).Attachments);
        Assert.Equal("caption", new MessageContent(new MessageText("caption"), [File()]).Text?.Value);
    }

    [Fact]
    public void Content_limits_the_number_of_attachments()
    {
        var max = Enumerable.Range(0, MessageContent.MaxAttachments).Select(_ => File()).ToArray();

        Assert.Equal(MessageContent.MaxAttachments, new MessageContent(null, max).Attachments.Count);
        Assert.Throws<DomainException>(() => new MessageContent(null, [..max, File()]));
    }

    [Fact]
    public void Content_rejects_the_same_file_twice()
    {
        var file = File();

        Assert.Throws<DomainException>(() => new MessageContent(null, [file, file]));
    }

    [Fact]
    public void Voice_message_has_nothing_else()
    {
        new MessageContent(null, [Voice()]);

        Assert.Throws<DomainException>(() => new MessageContent(new MessageText("hi"), [Voice()]));
        Assert.Throws<DomainException>(() => new MessageContent(null, [Voice(), File()]));
        Assert.Throws<DomainException>(() => new MessageContent(null, [Voice(), Voice()]));
    }

    [Fact]
    public void Message_keeps_its_attachments()
    {
        var alice = new ChatParticipant(Guid.NewGuid(), false, null);
        var chat = Chat.CreateDirect(alice, new ChatParticipant(Guid.NewGuid(), false, null));
        var voice = Voice();

        var message = chat.PostMessage(alice.UserId, new MessageContent(null, [voice]), 1, null);

        Assert.Null(message.Text);
        Assert.Same(voice, Assert.Single(message.Attachments));
    }
}
