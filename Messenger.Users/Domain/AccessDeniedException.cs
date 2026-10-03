namespace Messenger.Users.Domain;

public sealed class AccessDeniedException : DomainException
{
    public AccessDeniedException(string message)
        : base(message)
    {
    }
}
