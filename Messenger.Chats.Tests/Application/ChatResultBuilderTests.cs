using Messenger.Chats.Application;
using Messenger.Chats.Domain;
using Messenger.Users.Contracts;

namespace Messenger.Chats.Tests.Application;

public class ChatResultBuilderTests
{
    private static ChatParticipant User() => new(Guid.NewGuid(), false, null);

    private static async Task<ChatResult> Build(Chat chat, Guid viewerId, long lastSeq, long? readSeq)
    {
        var marks = new Dictionary<Guid, long>();

        if (readSeq != null)
            marks[chat.Id] = readSeq.Value;

        var builder = new ChatResultBuilder(
            new NoAccounts(),
            new FixedActivity(chat.Id, lastSeq),
            new FixedReadMarks(marks));

        var results = await builder.Build(viewerId, [chat], CancellationToken.None);

        return Assert.Single(results);
    }

    [Fact]
    public async Task Unread_counts_messages_after_the_mark()
    {
        var alice = User();
        var chat = Chat.CreateDirect(alice, User());

        var result = await Build(chat, alice.UserId, 5, 2);

        Assert.Equal((5L, 2L, 3L), (result.LastMessageSeq, result.LastReadSeq, result.UnreadCount));
    }

    [Fact]
    public async Task Member_without_a_mark_has_read_nothing()
    {
        var alice = User();
        var chat = Chat.CreateDirect(alice, User());

        var result = await Build(chat, alice.UserId, 5, null);

        Assert.Equal((0L, 5L), (result.LastReadSeq, result.UnreadCount));
    }

    private sealed class NoAccounts : IUsersApi
    {
        public Task<IReadOnlyList<AccountInfo>> GetAccounts(
            IReadOnlyCollection<Guid> accountIds,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<AccountInfo>>([]);
        }
    }

    private sealed class FixedReadMarks : IReadMarkRepository
    {
        private readonly IReadOnlyDictionary<Guid, long> _marks;

        public FixedReadMarks(IReadOnlyDictionary<Guid, long> marks)
        {
            _marks = marks;
        }

        public Task Advance(ReadMark mark, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyDictionary<Guid, long>> GetForMember(
            Guid userId,
            IReadOnlyCollection<Guid> chatIds,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_marks);
        }
    }

    private sealed class FixedActivity : IChatRepository
    {
        private readonly Guid _chatId;
        private readonly long _lastSeq;

        public FixedActivity(Guid chatId, long lastSeq)
        {
            _chatId = chatId;
            _lastSeq = lastSeq;
        }

        public Task<IReadOnlyDictionary<Guid, ChatActivity>> GetActivity(
            IReadOnlyCollection<Guid> chatIds,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyDictionary<Guid, ChatActivity>>(
                new Dictionary<Guid, ChatActivity> { [_chatId] = new(_lastSeq, DateTime.UtcNow) });
        }

        public Task<Chat?> Get(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<Chat?> GetDirect(string directKey, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<Chat>> GetByMember(Guid userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task Add(Chat chat, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task Update(Chat chat, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<long> NextMessageSeq(Guid chatId, DateTime sentAt, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
