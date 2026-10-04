namespace Messenger.Files.Domain;

public class StoredFile : AggregateRoot
{
    public const long MaxSize = 1024 * 1024;

    // An upload that no message picked up within this time is removed.
    public static readonly TimeSpan UnattachedLifetime = TimeSpan.FromHours(24);

    private StoredFile()
    {
    }


    public static StoredFile Upload(
        Guid ownerId,
        FileName fileName,
        MediaType contentType,
        long size,
        DateTime now)
    {
        if (size <= 0)
            throw new DomainException("The file is empty.");

        if (size > MaxSize)
            throw new FileTooLargeException(MaxSize);

        return new StoredFile
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            FileName = fileName,
            ContentType = contentType,
            Size = size,
            CreatedAt = now
        };
    }

    public bool IsOwnedBy(Guid accountId)
    {
        return OwnerId == accountId;
    }

    // Attaching again changes nothing: the same file may be sent in several messages.
    public void MarkAttached(DateTime now)
    {
        AttachedAt ??= now;
    }

    public Guid Id { get; private set; }

    public Guid OwnerId { get; private set; }

    public FileName FileName { get; private set; } = null!;

    public MediaType ContentType { get; private set; } = null!;

    public long Size { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? AttachedAt { get; private set; }
}
