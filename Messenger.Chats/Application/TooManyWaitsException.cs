namespace Messenger.Chats.Application;

public sealed class TooManyWaitsException : Exception
{
    public TooManyWaitsException()
        : base($"At most {WaitSlots.MaxPerAccount} waits can be open at once for one account.")
    {
    }
}
