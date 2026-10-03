namespace Messenger.Users.Domain;

public sealed class EmailAlreadyTakenException : DomainException
{
    public EmailAlreadyTakenException(Email email)
        : base($"Email '{email}' is already taken.")
    {
        Email = email;
    }

    public Email Email { get; }
}
