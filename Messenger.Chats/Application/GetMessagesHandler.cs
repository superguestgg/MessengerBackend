using Mediator;
using Messenger.Chats.Domain;

namespace Messenger.Chats.Application;

public sealed class GetMessagesHandler
    : IRequestHandler<GetMessagesQuery, IReadOnlyList<MessageResult>>
{
    private readonly IChatRepository _chatRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly MessageResultBuilder _resultBuilder;
    private readonly TimeProvider _time;

    public GetMessagesHandler(
        IChatRepository chatRepository,
        IMessageRepository messageRepository,
        MessageResultBuilder resultBuilder,
        TimeProvider time)
    {
        _chatRepository = chatRepository;
        _messageRepository = messageRepository;
        _resultBuilder = resultBuilder;
        _time = time;
    }


    public async ValueTask<IReadOnlyList<MessageResult>> Handle(
        GetMessagesQuery request,
        CancellationToken cancellationToken)
    {
        if (request.AfterSeq != null && request.BeforeSeq != null)
            throw new DomainException("Use either 'after' or 'before', not both.");

        var chat = await _chatRepository
            .Get(request.ChatId, cancellationToken);

        if (chat == null)
            throw new ChatNotFoundException(request.ChatId);

        chat.EnsureMember(request.UserId);


        var messages = await _messageRepository.GetPage(
            chat.Id,
            request.AfterSeq,
            request.BeforeSeq,
            request.Limit,
            cancellationToken);

        // Clients poll with "after", so it must not skip a message that is still being written.
        if (request.AfterSeq != null)
            messages = ContiguousMessages
                .Take(messages, request.AfterSeq.Value, _time.GetUtcNow().UtcDateTime)
                .Messages;


        return await _resultBuilder.Build(messages, cancellationToken);
    }
}
