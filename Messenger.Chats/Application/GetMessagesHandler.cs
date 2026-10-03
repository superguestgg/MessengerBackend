using Mediator;
using Messenger.Chats.Domain;
using Messenger.Users.Contracts;

namespace Messenger.Chats.Application;

public sealed class GetMessagesHandler
    : IRequestHandler<GetMessagesQuery, IReadOnlyList<MessageResult>>
{
    private readonly IChatRepository _chatRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IUsersApi _usersApi;

    public GetMessagesHandler(
        IChatRepository chatRepository,
        IMessageRepository messageRepository,
        IUsersApi usersApi)
    {
        _chatRepository = chatRepository;
        _messageRepository = messageRepository;
        _usersApi = usersApi;
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

        var authors = (await _usersApi.GetAccounts(
                messages.Select(x => x.AuthorId).Distinct().ToArray(),
                cancellationToken))
            .ToDictionary(x => x.AccountId);


        return messages
            .Select(x =>
            {
                var author = authors.GetValueOrDefault(x.AuthorId);

                return new MessageResult(
                    x.Id,
                    x.ChatId,
                    x.Seq,
                    x.AuthorId,
                    author?.DisplayName,
                    author?.IsBot ?? false,
                    x.Text.Value,
                    x.ReplyToSeq,
                    x.CreatedAt);
            })
            .ToArray();
    }
}
