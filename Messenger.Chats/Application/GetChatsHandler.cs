using Mediator;
using Messenger.Chats.Domain;

namespace Messenger.Chats.Application;

public sealed class GetChatsHandler
    : IRequestHandler<GetChatsQuery, IReadOnlyList<ChatResult>>
{
    private readonly IChatRepository _chatRepository;
    private readonly ChatResultBuilder _resultBuilder;

    public GetChatsHandler(
        IChatRepository chatRepository,
        ChatResultBuilder resultBuilder)
    {
        _chatRepository = chatRepository;
        _resultBuilder = resultBuilder;
    }


    public async ValueTask<IReadOnlyList<ChatResult>> Handle(
        GetChatsQuery request,
        CancellationToken cancellationToken)
    {
        var chats = await _chatRepository
            .GetByMember(request.UserId, cancellationToken);

        var results = await _resultBuilder.Build(chats, cancellationToken);

        // Most recently active first; chats without messages by creation time.
        return results
            .OrderByDescending(x => x.LastMessageAt ?? x.CreatedAt)
            .ToArray();
    }
}
