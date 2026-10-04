using Mediator;
using Messenger.Chats.Domain;

namespace Messenger.Chats.Application;

// The author has read everything up to their own message, so it never counts as unread for them.
public sealed class MarkReadOnMessageSent
    : INotificationHandler<MessageSent>
{
    private readonly IReadMarkRepository _readMarkRepository;

    public MarkReadOnMessageSent(IReadMarkRepository readMarkRepository)
    {
        _readMarkRepository = readMarkRepository;
    }


    public async ValueTask Handle(
        MessageSent notification,
        CancellationToken cancellationToken)
    {
        await _readMarkRepository.Advance(
            new ReadMark(notification.ChatId, notification.AuthorId, notification.Seq),
            cancellationToken);
    }
}
