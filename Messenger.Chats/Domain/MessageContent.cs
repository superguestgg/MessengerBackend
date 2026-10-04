namespace Messenger.Chats.Domain;

// What a message carries. Built before a sequence number is reserved,
// so a message that breaks these rules does not leave a gap in the numbering.
public sealed record MessageContent
{
    public const int MaxAttachments = 10;

    public MessageContent(
        MessageText? text,
        IReadOnlyList<Attachment> attachments)
    {
        if (text == null && attachments.Count == 0)
            throw new DomainException("A message needs text or attachments.");

        if (attachments.Count > MaxAttachments)
            throw new DomainException($"A message can have at most {MaxAttachments} attachments.");

        if (attachments.DistinctBy(x => x.FileId).Count() != attachments.Count)
            throw new DomainException("The same file is attached more than once.");

        if (attachments.Any(x => x.Kind == AttachmentKind.Voice) && (attachments.Count > 1 || text != null))
            throw new DomainException("A voice message has no text and no other attachments.");

        Text = text;
        Attachments = attachments;
    }

    public MessageText? Text { get; }

    public IReadOnlyList<Attachment> Attachments { get; }
}
