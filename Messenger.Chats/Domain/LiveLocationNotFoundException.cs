namespace Messenger.Chats.Domain;

public sealed class LiveLocationNotFoundException : DomainException
{
    public LiveLocationNotFoundException(long seq)
        : base($"Message #{seq} is not a live location.")
    {
        Seq = seq;
    }

    public long Seq { get; }
}
