using System.Collections.Concurrent;
using Messenger.Chats.Application;

namespace Messenger.Chats.Infrastructure;

// Works within one process. With several instances, waiters still get messages
// through the fallback poll, just later; a shared bus would replace this then.
public sealed class InMemoryChatSignals : IChatSignals
{
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource> _signals = new();

    public Task Next(Guid chatId)
    {
        return _signals
            .GetOrAdd(chatId, _ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously))
            .Task;
    }

    public void Notify(Guid chatId)
    {
        if (_signals.TryRemove(chatId, out var signal))
            signal.TrySetResult();
    }
}
