using Messenger.Chats.Domain;
using Messenger.Users.Contracts;

namespace Messenger.Chats.Application;

// Names are not stored in chats; they are looked up in one batch per response.
public sealed class ChatResultBuilder
{
    private readonly IUsersApi _usersApi;
    private readonly IChatRepository _chatRepository;
    private readonly IReadMarkRepository _readMarkRepository;

    public ChatResultBuilder(
        IUsersApi usersApi,
        IChatRepository chatRepository,
        IReadMarkRepository readMarkRepository)
    {
        _usersApi = usersApi;
        _chatRepository = chatRepository;
        _readMarkRepository = readMarkRepository;
    }

    // viewerId: whose read marks to show.
    public async Task<IReadOnlyList<ChatResult>> Build(
        Guid viewerId,
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

        var chatIds = chats.Select(x => x.Id).ToArray();

        var activity = await _chatRepository
            .GetActivity(chatIds, cancellationToken);

        var readMarks = await _readMarkRepository
            .GetForMember(viewerId, chatIds, cancellationToken);

        return chats
            .Select(chat =>
            {
                var chatActivity = activity.GetValueOrDefault(chat.Id);

                var lastSeq = chatActivity?.LastMessageSeq ?? 0;

                // Members who joined before read marks existed have none: their history counts as read.
                var lastReadSeq = readMarks.TryGetValue(chat.Id, out var mark)
                    ? Math.Min(mark, lastSeq)
                    : lastSeq;

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
                    lastSeq,
                    chatActivity?.LastMessageAt,
                    lastReadSeq,
                    // Seq numbers skipped by failed sends are counted too: a rare overcount, not worth a query.
                    lastSeq - lastReadSeq,
                    chat.CreatedAt);
            })
            .ToArray();
    }
}
