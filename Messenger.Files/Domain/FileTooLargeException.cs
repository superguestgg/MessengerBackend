namespace Messenger.Files.Domain;

public sealed class FileTooLargeException : DomainException
{
    public FileTooLargeException(long maxSize)
        : base($"A file must not be larger than {maxSize / 1024} KB.")
    {
    }
}
