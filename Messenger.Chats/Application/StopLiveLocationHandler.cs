using Mediator;
using Messenger.Chats.Domain;

namespace Messenger.Chats.Application;

public sealed class StopLiveLocationHandler
    : IRequestHandler<StopLiveLocationCommand>
{
    private readonly IChatRepository _chatRepository;
    private readonly ILiveLocationRepository _liveLocationRepository;
    private readonly TimeProvider _time;

    public StopLiveLocationHandler(
        IChatRepository chatRepository,
        ILiveLocationRepository liveLocationRepository,
        TimeProvider time)
    {
        _chatRepository = chatRepository;
        _liveLocationRepository = liveLocationRepository;
        _time = time;
    }


    public async ValueTask<Unit> Handle(
        StopLiveLocationCommand request,
        CancellationToken cancellationToken)
    {
        var live = await _liveLocationRepository
            .Get(request.ChatId, request.Seq, cancellationToken);

        // The author can stop their broadcast even after leaving the chat, e.g. from the banner;
        // anyone else learns whether it exists only as a member.
        if (live == null || live.UserId != request.UserId)
        {
            var chat = await _chatRepository
                .Get(request.ChatId, cancellationToken);

            if (chat == null)
                throw new ChatNotFoundException(request.ChatId);

            chat.EnsureMember(request.UserId);

            if (live == null)
                throw new LiveLocationNotFoundException(request.Seq);
        }

        // Already ended: someone stopped it a moment earlier, which is what the caller wanted.
        try
        {
            if (live.Stop(request.UserId, _time.GetUtcNow().UtcDateTime))
                await _liveLocationRepository.Update(live, cancellationToken);
        }
        catch (LiveLocationEndedException)
        {
        }


        return Unit.Value;
    }
}
