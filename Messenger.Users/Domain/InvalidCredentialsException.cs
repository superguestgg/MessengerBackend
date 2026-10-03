namespace Messenger.Users.Domain;

public sealed class InvalidCredentialsException : DomainException
{
    public InvalidCredentialsException()
        : base("Email or password is incorrect.")
    {
    }
}
