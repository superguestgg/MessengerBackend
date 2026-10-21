namespace Messenger.Chats.Domain;

// What a message carries. Built before a sequence number is reserved,
// so a message that breaks these rules does not leave a gap in the numbering.
public sealed record MessageContent
{
    public const int MaxAttachments = 10;

    public MessageContent(
        MessageText? text,
        IReadOnlyList<Attachment> attachments)
        : this(text, attachments, null, null)
    {
    }

    // With a live period the author keeps sending new points
    // until the period runs out or they stop it.
    public MessageContent(
        MessageText? text,
        IReadOnlyList<Attachment> attachments,
        GeoPoint? location,
        TimeSpan? livePeriod)
    {
        if (text == null && attachments.Count == 0 && location == null)
            throw new DomainException("A message needs text, attachments or a location.");

        if (attachments.Count > MaxAttachments)
            throw new DomainException($"A message can have at most {MaxAttachments} attachments.");

        if (attachments.DistinctBy(x => x.FileId).Count() != attachments.Count)
            throw new DomainException("The same file is attached more than once.");

        if (attachments.Any(x => x.Kind == AttachmentKind.Voice) && (attachments.Count > 1 || text != null))
            throw new DomainException("A voice message has no text and no other attachments.");

        if (location != null && (text != null || attachments.Count > 0))
            throw new DomainException("A location message has no text and no attachments.");

        if (livePeriod != null && location == null)
            throw new DomainException("A live period needs a location.");

        if (livePeriod != null
            && (livePeriod < TimeSpan.FromSeconds(LiveLocation.MinPeriodSeconds)
                || livePeriod > TimeSpan.FromSeconds(LiveLocation.MaxPeriodSeconds)))
            throw new DomainException(
                $"A live location lasts {LiveLocation.MinPeriodSeconds} to {LiveLocation.MaxPeriodSeconds} seconds.");

        Text = text;
        Attachments = attachments;
        Location = location;
        LivePeriod = livePeriod;
    }

    public MessageText? Text { get; }

    public IReadOnlyList<Attachment> Attachments { get; }

    public GeoPoint? Location { get; }

    public TimeSpan? LivePeriod { get; }
}
