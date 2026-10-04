using Mediator;
using Messenger.Chats.Domain;

namespace Messenger.Chats.Application;

public sealed class MarkChatReadHandler
    : IRequestHandler<MarkChatReadCommand>
{
    private readonly IChatRepository _chatRepository;
    private readonly IReadMarkRepository _readMarkRepository;

    public MarkChatReadHandler(
        IChatRepository chatRepository,
        IReadMarkRepository readMarkRepository)
    {
        _chatRepository = chatRepository;
        _readMarkRepository = readMarkRepository;
    }


    public async ValueTask<Unit> Handle(
        MarkChatReadCommand request,
        CancellationToken cancellationToken)
    {
        var chat = await _chatRepository
            .Get(request.ChatId, cancellationToken);

        if (chat == null)
            throw new ChatNotFoundException(request.ChatId);

        var activity = await _chatRepository
            .GetActivity([chat.Id], cancellationToken);

        var mark = chat.MarkRead(
            request.UserId,
            request.Seq,
            activity.GetValueOrDefault(chat.Id)?.LastMessageSeq ?? 0);


        await _readMarkRepository.Advance(mark, cancellationToken);


        return Unit.Value;
    }
}
