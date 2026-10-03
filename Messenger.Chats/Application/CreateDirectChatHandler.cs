using Mediator;
using Messenger.Chats.Domain;

namespace Messenger.Chats.Application;

// Idempotent: an existing direct chat between the two accounts is returned instead of a new one.
public sealed class CreateDirectChatHandler
    : IRequestHandler<CreateDirectChatCommand, CreateChatResult>
{
    private readonly IChatRepository _chatRepository;
    private readonly ParticipantLookup _participants;

    public CreateDirectChatHandler(
        IChatRepository chatRepository,
        ParticipantLookup participants)
    {
        _chatRepository = chatRepository;
        _participants = participants;
    }


    public async ValueTask<CreateChatResult> Handle(
        CreateDirectChatCommand request,
        CancellationToken cancellationToken)
    {
        var participants = await _participants
            .GetActive([request.RequesterId, request.UserId], cancellationToken);

        var chat = Chat.CreateDirect(participants[0], participants[1]);

        var existing = await _chatRepository
            .GetDirect(chat.DirectKey!, cancellationToken);

        if (existing != null)
            return new CreateChatResult(existing.Id);


        try
        {
            await _chatRepository.Add(chat, cancellationToken);
        }
        catch (DirectChatAlreadyExistsException)
        {
            existing = await _chatRepository.GetDirect(chat.DirectKey!, cancellationToken);

            return new CreateChatResult(existing!.Id);
        }


        return new CreateChatResult(chat.Id);
    }
}
