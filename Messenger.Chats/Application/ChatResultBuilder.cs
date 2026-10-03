using Messenger.Chats.Domain;
using Messenger.Users.Contracts;

namespace Messenger.Chats.Application;

// Names are not stored in chats; they are looked up in one batch per response.
public sealed class ChatResultBuilder
{
    private readonly IUsersApi _usersApi;
    private readonly IChatRepository _chatRepository;

    public ChatResultBuilder(
        IUsersApi usersApi,
        IChatRepository chatRepository)
    {
        _usersApi = usersApi;
        _chatRepository = chatRepository;
    }

    public async Task<IReadOnlyList<ChatResult>> Build(
        IReadOnlyList<Chat> chats,
        CancellationToken cancellationToken)
    {
        var userIds = chats
            .SelectMany(x => x.Members)
            .Select(x => x.UserId)
            .Distinct()
            .ToArray();

        var accounts = (await _usersApi.GetAccounts(userIds, cancellationToken))
            .ToDictionary(x => x.AccountId);

        var activity = await _chatRepository
            .GetActivity(chats.Select(x => x.Id).ToArray(), cancellationToken);

        return chats
            .Select(chat =>
            {
                var chatActivity = activity.GetValueOrDefault(chat.Id);

                return new ChatResult(
                    chat.Id,
                    chat.Type,
                    chat.Title?.Value,
                    chat.Members
                        .Select(member =>
                        {
                            var account = accounts.GetValueOrDefault(member.UserId);

                            return new ChatMemberResult(
                                member.UserId,
                                account?.DisplayName,
                                account?.IsBot ?? false,
                                member.Role,
                                member.JoinedAt);
                        })
                        .ToArray(),
                    chatActivity?.LastMessageSeq ?? 0,
                    chatActivity?.LastMessageAt,
                    chat.CreatedAt);
            })
            .ToArray();
    }
}
