using Mediator;
using Messenger.Chats.Domain;

namespace Messenger.Chats.Application;

public sealed class RemoveChatMemberHandler
    : IRequestHandler<RemoveChatMemberCommand>
{
    private readonly IChatRepository _chatRepository;
    private readonly IPublisher _publisher;

    public RemoveChatMemberHandler(
        IChatRepository chatRepository,
        IPublisher publisher)
    {
        _chatRepository = chatRepository;
        _publisher = publisher;
    }


    public async ValueTask<Unit> Handle(
        RemoveChatMemberCommand request,
        CancellationToken cancellationToken)
    {
        var chat = await _chatRepository
            .Get(request.ChatId, cancellationToken);

        if (chat == null)
            throw new ChatNotFoundException(request.ChatId);


        chat.RemoveMember(request.RequesterId, request.UserId);


        await _chatRepository.Update(chat, cancellationToken);

        await _publisher.PublishDomainEvents(chat, cancellationToken);


        return Unit.Value;
    }
}
