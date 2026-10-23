using Mediator;
using Messenger.Chats.Domain;

namespace Messenger.Chats.Application;

public sealed class GetChatLiveLocationsHandler
    : IRequestHandler<GetChatLiveLocationsQuery, IReadOnlyList<LiveLocationResult>>
{
    private readonly IChatRepository _chatRepository;
    private readonly ILiveLocationRepository _liveLocationRepository;
    private readonly TimeProvider _time;

    public GetChatLiveLocationsHandler(
        IChatRepository chatRepository,
        ILiveLocationRepository liveLocationRepository,
        TimeProvider time)
    {
        _chatRepository = chatRepository;
        _liveLocationRepository = liveLocationRepository;
        _time = time;
    }


    public async ValueTask<IReadOnlyList<LiveLocationResult>> Handle(
        GetChatLiveLocationsQuery request,
        CancellationToken cancellationToken)
    {
        var chat = await _chatRepository
            .Get(request.ChatId, cancellationToken);

        if (chat == null)
            throw new ChatNotFoundException(request.ChatId);

        chat.EnsureMember(request.UserId);

        var now = _time.GetUtcNow().UtcDateTime;

        var live = await _liveLocationRepository
            .GetActiveInChat(chat.Id, now, cancellationToken);

        // A member who left keeps the broadcast in "my live locations" to stop it, but the chat no longer sees it.
        return live
            .Where(x => chat.IsMember(x.UserId))
            .Select(x => LocationResults.ToLiveResult(x, now))
            .ToArray();
    }
}
