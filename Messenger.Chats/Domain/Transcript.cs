namespace Messenger.Chats.Domain;

// Text of a voice message, filled in by speech recognition after the message is sent.
public class Transcript
{
    private Transcript()
    {
    }


    internal static Transcript None()
    {
        return new Transcript
        {
            Status = TranscriptStatus.None
        };
    }

    public TranscriptStatus Status { get; private set; }

    // Only when Status is Done.
    public string? Text { get; private set; }
}
