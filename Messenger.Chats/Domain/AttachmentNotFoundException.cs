namespace Messenger.Chats.Domain;

// Also thrown for someone else's upload, so file ids cannot be probed.
public sealed class AttachmentNotFoundException : DomainException
{
    public AttachmentNotFoundException(Guid fileId)
        : base($"File '{fileId}' was not found.")
    {
        FileId = fileId;
    }

    public Guid FileId { get; }
}
