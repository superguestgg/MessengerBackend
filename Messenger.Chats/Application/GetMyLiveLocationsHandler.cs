using Mediator;
using Messenger.Chats.Domain;

namespace Messenger.Chats.Application;

public sealed class GetMyLiveLocationsHandler
    : IRequestHandler<GetMyLiveLocationsQuery, IReadOnlyList<LiveLocationResult>>
{
    private readonly ILiveLocationRepository _liveLocationRepository;
    private readonly TimeProvider _time;

    public GetMyLiveLocationsHandler(
        ILiveLocationRepository liveLocationRepository,
        TimeProvider time)
    {
        _liveLocationRepository = liveLocationRepository;
        _time = time;
    }


    public async ValueTask<IReadOnlyList<LiveLocationResult>> Handle(
        GetMyLiveLocationsQuery request,
        CancellationToken cancellationToken)
    {
        var now = _time.GetUtcNow().UtcDateTime;

        var live = await _liveLocationRepository
            .GetActiveByUser(request.UserId, now, cancellationToken);

        return live
            .OrderBy(x => x.ExpiresAt)
            .Select(x => LocationResults.ToLiveResult(x, now))
            .ToArray();
    }
}
