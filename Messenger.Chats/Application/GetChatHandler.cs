using Mediator;
using Messenger.Chats.Domain;

namespace Messenger.Chats.Application;

public sealed class GetChatHandler
    : IRequestHandler<GetChatQuery, ChatResult>
{
    private readonly IChatRepository _chatRepository;
    private readonly ChatResultBuilder _resultBuilder;

    public GetChatHandler(
        IChatRepository chatRepository,
        ChatResultBuilder resultBuilder)
    {
        _chatRepository = chatRepository;
        _resultBuilder = resultBuilder;
    }


    public async ValueTask<ChatResult> Handle(
        GetChatQuery request,
        CancellationToken cancellationToken)
    {
        var chat = await _chatRepository
            .Get(request.ChatId, cancellationToken);

        if (chat == null)
            throw new ChatNotFoundException(request.ChatId);

        chat.EnsureMember(request.UserId);

        var results = await _resultBuilder.Build(request.UserId, [chat], cancellationToken);

        return results[0];
    }
}
