using Mediator;
using Messenger.Chats.Domain;

namespace Messenger.Chats.Application;

// A member who left can no longer send points to the chat, so their broadcasts there end
// instead of hanging as live until they run out.
public sealed class StopLiveLocationsOnMemberLeft
    : INotificationHandler<ChatMemberLeft>
{
    private readonly ILiveLocationRepository _liveLocationRepository;
    private readonly TimeProvider _time;

    public StopLiveLocationsOnMemberLeft(
        ILiveLocationRepository liveLocationRepository,
        TimeProvider time)
    {
        _liveLocationRepository = liveLocationRepository;
        _time = time;
    }


    public async ValueTask Handle(
        ChatMemberLeft notification,
        CancellationToken cancellationToken)
    {
        var now = _time.GetUtcNow().UtcDateTime;

        var live = await _liveLocationRepository
            .GetActiveInChat(notification.ChatId, now, cancellationToken);

        foreach (var item in live.Where(x => x.UserId == notification.UserId))
        {
            // Already stopped from a device a moment earlier: nothing left to do.
            try
            {
                if (item.Stop(notification.UserId, now))
                    await _liveLocationRepository.Update(item, cancellationToken);
            }
            catch (LiveLocationEndedException)
            {
            }
        }
    }
}
