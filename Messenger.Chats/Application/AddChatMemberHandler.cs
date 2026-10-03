using Mediator;
using Messenger.Chats.Domain;

namespace Messenger.Chats.Application;

public sealed class AddChatMemberHandler
    : IRequestHandler<AddChatMemberCommand>
{
    private readonly IChatRepository _chatRepository;
    private readonly ParticipantLookup _participants;

    public AddChatMemberHandler(
        IChatRepository chatRepository,
        ParticipantLookup participants)
    {
        _chatRepository = chatRepository;
        _participants = participants;
    }


    public async ValueTask<Unit> Handle(
        AddChatMemberCommand request,
        CancellationToken cancellationToken)
    {
        var chat = await _chatRepository
            .Get(request.ChatId, cancellationToken);

        if (chat == null)
            throw new ChatNotFoundException(request.ChatId);

        // Membership first, so outsiders cannot use this endpoint to probe account ids.
        chat.EnsureMember(request.RequesterId);

        var participant = await _participants
            .GetActive(request.UserId, cancellationToken);


        chat.AddMember(request.RequesterId, participant);


        await _chatRepository.Update(chat, cancellationToken);


        return Unit.Value;
    }
}
