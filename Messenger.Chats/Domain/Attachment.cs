namespace Messenger.Chats.Domain;

// A file from the Files module as a message carries it. A file never changes after upload,
// so its name, type and size are copied here instead of being looked up on every read.
public class Attachment
{
    public const int MaxVoiceDurationSeconds = 5 * 60;

    // Types every browser shows in <img>. SVG is left out: it can carry scripts.
    private static readonly HashSet<string> ImageTypes =
    [
        "image/png",
        "image/jpeg",
        "image/gif",
        "image/webp"
    ];

    private Attachment()
    {
    }


    public static Attachment File(
        Guid fileId,
        string fileName,
        string contentType,
        long size)
    {
        return new Attachment
        {
            FileId = fileId,
            Kind = ImageTypes.Contains(contentType) ? AttachmentKind.Image : AttachmentKind.File,
            FileName = fileName,
            ContentType = contentType,
            Size = size
        };
    }

    // The duration comes from the recording client; it is shown before the audio is loaded.
    public static Attachment Voice(
        Guid fileId,
        string fileName,
        string contentType,
        long size,
        int durationSeconds)
    {
        if (!contentType.StartsWith("audio/", StringComparison.Ordinal))
            throw new DomainException("A voice message must be an audio file.");

        if (durationSeconds < 1 || durationSeconds > MaxVoiceDurationSeconds)
            throw new DomainException(
                $"A voice message must be 1 to {MaxVoiceDurationSeconds} seconds long.");

        return new Attachment
        {
            FileId = fileId,
            Kind = AttachmentKind.Voice,
            FileName = fileName,
            ContentType = contentType,
            Size = size,
            DurationSeconds = durationSeconds,
            Transcript = Transcript.None()
        };
    }

    // The frontend is served from the same origin, so only types a browser renders safely
    // are shown in place; anything else is a download, or an uploaded HTML page would run as ours.
    public bool CanBeShownInline => Kind != AttachmentKind.File;

    public Guid FileId { get; private set; }

    public AttachmentKind Kind { get; private set; }

    public string FileName { get; private set; } = null!;

    public string ContentType { get; private set; } = null!;

    public long Size { get; private set; }

    // Voice only.
    public int? DurationSeconds { get; private set; }

    // Voice only.
    public Transcript? Transcript { get; private set; }
}
