using System.Collections.Concurrent;

namespace Messenger.Chats.Application;

// Caps how many waits one account holds open, so a client cannot tie up the server's connections.
public sealed class WaitSlots
{
    public const int MaxPerAccount = 5;

    private readonly ConcurrentDictionary<Guid, int> _inUse = new();

    public IDisposable? TryAcquire(Guid accountId)
    {
        while (true)
        {
            var current = _inUse.GetOrAdd(accountId, 0);

            if (current >= MaxPerAccount)
                return null;

            if (_inUse.TryUpdate(accountId, current + 1, current))
                return new Slot(this, accountId);
        }
    }

    private void Release(Guid accountId)
    {
        while (true)
        {
            var current = _inUse[accountId];

            if (current == 1)
            {
                if (_inUse.TryRemove(new KeyValuePair<Guid, int>(accountId, 1)))
                    return;
            }
            else if (_inUse.TryUpdate(accountId, current - 1, current))
            {
                return;
            }
        }
    }

    private sealed class Slot : IDisposable
    {
        private readonly WaitSlots _owner;
        private readonly Guid _accountId;
        private int _released;

        public Slot(WaitSlots owner, Guid accountId)
        {
            _owner = owner;
            _accountId = accountId;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _released, 1) == 0)
                _owner.Release(_accountId);
        }
    }
}
