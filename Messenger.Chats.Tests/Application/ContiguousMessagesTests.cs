using Messenger.Chats.Application;
using Messenger.Chats.Domain;

namespace Messenger.Chats.Tests.Application;

public class ContiguousMessagesTests
{
    private readonly ChatParticipant _alice = new(Guid.NewGuid(), false, null);
    private readonly Chat _chat;

    public ContiguousMessagesTests()
    {
        _chat = Chat.CreateDirect(_alice, new ChatParticipant(Guid.NewGuid(), false, null));
    }

    private Message Message(long seq)
    {
        return _chat.PostMessage(_alice.UserId, new MessageText($"#{seq}"), seq, null);
    }

    private static DateTime Soon => DateTime.UtcNow;

    private static DateTime Later => DateTime.UtcNow + ContiguousMessages.GapGracePeriod + TimeSpan.FromSeconds(1);

    [Fact]
    public void Returns_everything_when_there_are_no_gaps()
    {
        var (messages, next, blocked) = ContiguousMessages.Take([Message(4), Message(5), Message(6)], 3, Soon);

        Assert.Equal([4L, 5L, 6L], messages.Select(x => x.Seq));
        Assert.Equal(6, next);
        Assert.False(blocked);
    }

    [Fact]
    public void Empty_page_keeps_the_cursor()
    {
        var (messages, next, blocked) = ContiguousMessages.Take([], 7, Soon);

        Assert.Empty(messages);
        Assert.Equal(7, next);
        Assert.False(blocked);
    }

    [Fact]
    public void Stops_before_a_young_gap()
    {
        var (messages, next, blocked) = ContiguousMessages.Take([Message(4), Message(6), Message(7)], 3, Soon);

        Assert.Equal([4L], messages.Select(x => x.Seq));
        Assert.Equal(4, next);
        Assert.True(blocked);
    }

    [Fact]
    public void Young_gap_right_after_the_cursor_returns_nothing()
    {
        var (messages, next, blocked) = ContiguousMessages.Take([Message(5)], 3, Soon);

        Assert.Empty(messages);
        Assert.Equal(3, next);
        Assert.True(blocked);
    }

    [Fact]
    public void Skips_a_gap_once_it_is_old()
    {
        var (messages, next, blocked) = ContiguousMessages.Take([Message(4), Message(6), Message(9)], 3, Later);

        Assert.Equal([4L, 6L, 9L], messages.Select(x => x.Seq));
        Assert.Equal(9, next);
        Assert.False(blocked);
    }
}
