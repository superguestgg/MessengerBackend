namespace Messenger.Chats.Domain;

public enum TranscriptStatus
{
    // Speech recognition is off: the voice message stays without text.
    None,
    Pending,
    Done,
    Failed
}
