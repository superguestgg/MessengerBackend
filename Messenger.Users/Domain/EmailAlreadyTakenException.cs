namespace Messenger.Users.Domain;

public sealed class EmailAlreadyTakenException : Exception
{
    public EmailAlreadyTakenException(string email)
        : base($"Email '{email}' is already taken.")
    {
        Email = email;
    }

    public string Email { get; }
}
