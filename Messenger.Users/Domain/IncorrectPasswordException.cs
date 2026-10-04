namespace Messenger.Users.Domain;

// Not InvalidCredentialsException: the caller is signed in, and a 401 would end their session in the frontend.
public sealed class IncorrectPasswordException : DomainException
{
    public IncorrectPasswordException()
        : base("Current password is incorrect.")
    {
    }
}
