namespace Messenger.Chats.Domain;

public interface IChatRepository
{
    Task<Chat?> Get(Guid id, CancellationToken cancellationToken = default);

    Task<Chat?> GetDirect(string directKey, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Chat>> GetByMember(Guid userId, CancellationToken cancellationToken = default);

    // Throws DirectChatAlreadyExistsException if another request created the same direct chat first.
    Task Add(Chat chat, CancellationToken cancellationToken = default);

    // Throws ChatConcurrencyException if the chat changed since it was loaded.
    Task Update(Chat chat, CancellationToken cancellationToken = default);

    // Atomically reserves the next message number in the chat.
    Task<long> NextMessageSeq(Guid chatId, DateTime sentAt, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, ChatActivity>> GetActivity(IReadOnlyCollection<Guid> chatIds, CancellationToken cancellationToken = default);
}
