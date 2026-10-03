using Mediator;
using Messenger.Chats.Domain;

namespace Messenger.Chats.Application;

public sealed class RemoveChatMemberHandler
    : IRequestHandler<RemoveChatMemberCommand>
{
    private readonly IChatRepository _chatRepository;

    public RemoveChatMemberHandler(IChatRepository chatRepository)
    {
        _chatRepository = chatRepository;
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


        return Unit.Value;
    }
}
