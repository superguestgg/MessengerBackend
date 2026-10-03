using Messenger.Chats.Infrastructure;

namespace Messenger.Chats.Tests.Infrastructure;

public class InMemoryChatSignalsTests
{
    [Fact]
    public void Notify_completes_waiters_of_that_chat_only()
    {
        var signals = new InMemoryChatSignals();
        var chat = Guid.NewGuid();

        var first = signals.Next(chat);
        var second = signals.Next(chat);
        var other = signals.Next(Guid.NewGuid());

        signals.Notify(chat);

        Assert.True(first.Wait(TimeSpan.FromSeconds(1)));
        Assert.True(second.Wait(TimeSpan.FromSeconds(1)));
        Assert.False(other.IsCompleted);
    }

    [Fact]
    public void Signal_taken_after_notify_waits_for_the_next_one()
    {
        var signals = new InMemoryChatSignals();
        var chat = Guid.NewGuid();

        signals.Notify(chat);
        var next = signals.Next(chat);

        Assert.False(next.IsCompleted);

        signals.Notify(chat);

        Assert.True(next.Wait(TimeSpan.FromSeconds(1)));
    }
}
