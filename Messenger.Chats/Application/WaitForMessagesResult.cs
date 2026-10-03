namespace Messenger.Chats.Application;

// NextAfterSeq covers messages that were checked and did not pass the filters,
// so the next wait does not scan them again. Empty Messages means the wait timed out.
public sealed record WaitForMessagesResult(
    IReadOnlyList<MessageResult> Messages,
    long NextAfterSeq
);
