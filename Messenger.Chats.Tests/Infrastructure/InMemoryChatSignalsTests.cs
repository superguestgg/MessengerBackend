using Messenger.Chats.Infrastructure;

namespace Messenger.Chats.Tests.Infrastructure;

public class InMemoryChatSignalsTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(1);

    [Fact]
    public async Task Notify_completes_waiters_of_that_chat_only()
    {
        var signals = new InMemoryChatSignals();
        var chat = Guid.NewGuid();

        var first = signals.Next(chat);
        var second = signals.Next(chat);
        var other = signals.Next(Guid.NewGuid());

        signals.Notify(chat);

        await first.WaitAsync(Timeout);
        await second.WaitAsync(Timeout);
        Assert.False(other.IsCompleted);
    }

    [Fact]
    public async Task Signal_taken_after_notify_waits_for_the_next_one()
    {
        var signals = new InMemoryChatSignals();
        var chat = Guid.NewGuid();

        signals.Notify(chat);
        var next = signals.Next(chat);

        Assert.False(next.IsCompleted);

        signals.Notify(chat);

        await next.WaitAsync(Timeout);
    }
}
