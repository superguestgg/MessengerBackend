using Mediator;
using Messenger.Chats.Domain;

namespace Messenger.Chats.Application;

public sealed class NotifyWaitersOnMessageSent
    : INotificationHandler<MessageSent>
{
    private readonly IChatSignals _signals;

    public NotifyWaitersOnMessageSent(IChatSignals signals)
    {
        _signals = signals;
    }


    public ValueTask Handle(
        MessageSent notification,
        CancellationToken cancellationToken)
    {
        _signals.Notify(notification.ChatId);

        return ValueTask.CompletedTask;
    }
}
