using Mediator;
using Messenger.Chats.Domain;

namespace Messenger.Chats.Application;

public sealed class CreateGroupChatHandler
    : IRequestHandler<CreateGroupChatCommand, CreateChatResult>
{
    private readonly IChatRepository _chatRepository;
    private readonly ParticipantLookup _participants;

    public CreateGroupChatHandler(
        IChatRepository chatRepository,
        ParticipantLookup participants)
    {
        _chatRepository = chatRepository;
        _participants = participants;
    }


    public async ValueTask<CreateChatResult> Handle(
        CreateGroupChatCommand request,
        CancellationToken cancellationToken)
    {
        var creator = await _participants
            .GetActive(request.RequesterId, cancellationToken);

        var members = await _participants
            .GetActive(request.MemberIds.Distinct().ToArray(), cancellationToken);


        var chat = Chat.CreateGroup(
            creator,
            new ChatTitle(request.Title),
            members);


        await _chatRepository.Add(chat, cancellationToken);


        return new CreateChatResult(chat.Id);
    }
}
