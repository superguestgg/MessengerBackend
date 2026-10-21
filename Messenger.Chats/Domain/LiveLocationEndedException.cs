namespace Messenger.Chats.Domain;

// Tells the sending device to stop: the broadcast ran out or was stopped, maybe from another device.
public sealed class LiveLocationEndedException : DomainException
{
    public LiveLocationEndedException()
        : base("The live location has ended.")
    {
    }
}
