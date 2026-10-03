using Mediator;
using Messenger.Chats.Domain;

namespace Messenger.Chats.Application;

public sealed class ChangeChatMemberRoleHandler
    : IRequestHandler<ChangeChatMemberRoleCommand>
{
    private readonly IChatRepository _chatRepository;

    public ChangeChatMemberRoleHandler(IChatRepository chatRepository)
    {
        _chatRepository = chatRepository;
    }


    public async ValueTask<Unit> Handle(
        ChangeChatMemberRoleCommand request,
        CancellationToken cancellationToken)
    {
        var chat = await _chatRepository
            .Get(request.ChatId, cancellationToken);

        if (chat == null)
            throw new ChatNotFoundException(request.ChatId);


        chat.ChangeRole(request.RequesterId, request.UserId, request.Role);


        await _chatRepository.Update(chat, cancellationToken);


        return Unit.Value;
    }
}
