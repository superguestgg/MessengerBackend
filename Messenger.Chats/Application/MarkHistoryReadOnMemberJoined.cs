using Mediator;
using Messenger.Chats.Domain;

namespace Messenger.Chats.Application;

// A new member starts with the history already read: only messages after joining count as unread.
public sealed class MarkHistoryReadOnMemberJoined
    : INotificationHandler<ChatMemberJoined>
{
    private readonly IChatRepository _chatRepository;
    private readonly IReadMarkRepository _readMarkRepository;

    public MarkHistoryReadOnMemberJoined(
        IChatRepository chatRepository,
        IReadMarkRepository readMarkRepository)
    {
        _chatRepository = chatRepository;
        _readMarkRepository = readMarkRepository;
    }


    public async ValueTask Handle(
        ChatMemberJoined notification,
        CancellationToken cancellationToken)
    {
        var activity = await _chatRepository
            .GetActivity([notification.ChatId], cancellationToken);

        var lastSeq = activity.GetValueOrDefault(notification.ChatId)?.LastMessageSeq ?? 0;

        await _readMarkRepository.Advance(
            new ReadMark(notification.ChatId, notification.UserId, lastSeq),
            cancellationToken);
    }
}
