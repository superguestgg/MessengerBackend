using Mediator;
using Messenger.Chats.Domain;

namespace Messenger.Chats.Application;

public sealed class SendMessageHandler
    : IRequestHandler<SendMessageCommand, SendMessageResult>
{
    private readonly IChatRepository _chatRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IPublisher _publisher;

    public SendMessageHandler(
        IChatRepository chatRepository,
        IMessageRepository messageRepository,
        IPublisher publisher)
    {
        _chatRepository = chatRepository;
        _messageRepository = messageRepository;
        _publisher = publisher;
    }


    public async ValueTask<SendMessageResult> Handle(
        SendMessageCommand request,
        CancellationToken cancellationToken)
    {
        var chat = await _chatRepository
            .Get(request.ChatId, cancellationToken);

        if (chat == null)
            throw new ChatNotFoundException(request.ChatId);

        // Checked before a number is reserved, so outsiders cannot burn sequence numbers.
        chat.EnsureMember(request.AuthorId);

        var text = new MessageText(request.Text);

        Message? replyTo = null;

        if (request.ReplyToSeq != null)
            replyTo = await _messageRepository.GetBySeq(chat.Id, request.ReplyToSeq.Value, cancellationToken)
                      ?? throw new DomainException($"Message #{request.ReplyToSeq} was not found in this chat.");


        var seq = await _chatRepository
            .NextMessageSeq(chat.Id, DateTime.UtcNow, cancellationToken);

        var message = chat.PostMessage(request.AuthorId, text, seq, replyTo);


        await _messageRepository.Add(message, cancellationToken);

        await _publisher.PublishDomainEvents(message, cancellationToken);


        return new SendMessageResult(
            message.Id,
            message.ChatId,
            message.Seq,
            message.CreatedAt);
    }
}
