using Messenger.Files.Domain;

namespace Messenger.Files.Tests.Domain;

public class StoredFileTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static StoredFile Upload(Guid ownerId, long size = 100) =>
        StoredFile.Upload(ownerId, new FileName("a.txt"), new MediaType("text/plain"), size, Now);

    [Fact]
    public void Upload_keeps_metadata_and_is_not_attached()
    {
        var owner = Guid.NewGuid();

        var file = Upload(owner, 42);

        Assert.NotEqual(Guid.Empty, file.Id);
        Assert.Equal(owner, file.OwnerId);
        Assert.Equal("a.txt", file.FileName.Value);
        Assert.Equal("text/plain", file.ContentType.Value);
        Assert.Equal(42, file.Size);
        Assert.Equal(Now, file.CreatedAt);
        Assert.Null(file.AttachedAt);
        Assert.True(file.IsOwnedBy(owner));
        Assert.False(file.IsOwnedBy(Guid.NewGuid()));
    }

    [Fact]
    public void Size_is_limited_to_one_megabyte()
    {
        Assert.Equal(1024 * 1024, StoredFile.MaxSize);
        Assert.Equal(StoredFile.MaxSize, Upload(Guid.NewGuid(), StoredFile.MaxSize).Size);
        Assert.Throws<FileTooLargeException>(() => Upload(Guid.NewGuid(), StoredFile.MaxSize + 1));
    }

    [Fact]
    public void Empty_file_is_rejected()
    {
        Assert.Throws<DomainException>(() => Upload(Guid.NewGuid(), 0));
    }

    [Fact]
    public void MarkAttached_keeps_the_first_time()
    {
        var file = Upload(Guid.NewGuid());

        file.MarkAttached(Now.AddMinutes(1));
        file.MarkAttached(Now.AddMinutes(2));

        Assert.Equal(Now.AddMinutes(1), file.AttachedAt);
    }
}
