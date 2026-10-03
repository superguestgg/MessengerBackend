using Messenger.Chats.Domain;

namespace Messenger.Chats.Application;

// A message number is reserved before the message is written, so #N+1 can become
// visible after #N+2. A reader that jumped to "after N+2" would never see #N+1.
// Messages are therefore handed out only up to the first gap that may still fill.
public static class ContiguousMessages
{
    // A failed send leaves its number unused forever; after this long the gap is skipped.
    public static readonly TimeSpan GapGracePeriod = TimeSpan.FromSeconds(5);

    // messages: ascending by Seq, all newer than afterSeq.
    public static (IReadOnlyList<Message> Messages, long NextAfterSeq, bool BlockedByGap) Take(
        IReadOnlyList<Message> messages,
        long afterSeq,
        DateTime now)
    {
        var result = new List<Message>();

        var expected = afterSeq + 1;

        foreach (var message in messages)
        {
            if (message.Seq != expected && now - message.CreatedAt < GapGracePeriod)
                return (result, expected - 1, true);

            result.Add(message);

            expected = message.Seq + 1;
        }

        return (result, expected - 1, false);
    }
}
