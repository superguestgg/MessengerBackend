using Messenger.Chats.Application;

namespace Messenger.Chats.Tests.Application;

public class WaitSlotsTests
{
    [Fact]
    public void Limits_waits_per_account_and_frees_on_dispose()
    {
        var slots = new WaitSlots();
        var account = Guid.NewGuid();

        var taken = Enumerable.Range(0, WaitSlots.MaxPerAccount)
            .Select(_ => slots.TryAcquire(account))
            .ToArray();

        Assert.All(taken, Assert.NotNull);
        Assert.Null(slots.TryAcquire(account));
        Assert.NotNull(slots.TryAcquire(Guid.NewGuid()));

        taken[0]!.Dispose();
        taken[0]!.Dispose();

        Assert.NotNull(slots.TryAcquire(account));
        Assert.Null(slots.TryAcquire(account));
    }

    [Fact]
    public async Task Concurrent_acquire_never_exceeds_the_limit()
    {
        var slots = new WaitSlots();
        var account = Guid.NewGuid();

        var acquired = await Task.WhenAll(Enumerable.Range(0, 100)
            .Select(_ => Task.Run(() => slots.TryAcquire(account))));

        Assert.Equal(WaitSlots.MaxPerAccount, acquired.Count(x => x != null));
    }
}
