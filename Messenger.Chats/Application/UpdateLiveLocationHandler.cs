using Mediator;
using Messenger.Chats.Domain;

namespace Messenger.Chats.Application;

public sealed class UpdateLiveLocationHandler
    : IRequestHandler<UpdateLiveLocationCommand>
{
    private readonly IChatRepository _chatRepository;
    private readonly ILiveLocationRepository _liveLocationRepository;
    private readonly TimeProvider _time;

    public UpdateLiveLocationHandler(
        IChatRepository chatRepository,
        ILiveLocationRepository liveLocationRepository,
        TimeProvider time)
    {
        _chatRepository = chatRepository;
        _liveLocationRepository = liveLocationRepository;
        _time = time;
    }


    public async ValueTask<Unit> Handle(
        UpdateLiveLocationCommand request,
        CancellationToken cancellationToken)
    {
        var chat = await _chatRepository
            .Get(request.ChatId, cancellationToken);

        if (chat == null)
            throw new ChatNotFoundException(request.ChatId);

        // A member who left the chat stops broadcasting to it.
        chat.EnsureMember(request.UserId);

        var live = await _liveLocationRepository
                       .Get(chat.Id, request.Seq, cancellationToken)
                   ?? throw new LiveLocationNotFoundException(request.Seq);

        var point = new GeoPoint(request.Latitude, request.Longitude, request.AccuracyMeters);

        if (live.Update(request.UserId, point, _time.GetUtcNow().UtcDateTime))
            await _liveLocationRepository.Update(live, cancellationToken);


        return Unit.Value;
    }
}
