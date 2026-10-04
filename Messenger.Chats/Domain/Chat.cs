namespace Messenger.Chats.Domain;

public class Chat : AggregateRoot
{
    // Not readonly: the Mongo driver sets it when loading a chat.
    private List<ChatMember> _members = new();

    private Chat()
    {
    }


    public static Chat CreateDirect(
        ChatParticipant initiator,
        ChatParticipant other)
    {
        if (initiator.UserId == other.UserId)
            throw new DomainException("A direct chat needs two different accounts.");

        // Bots do not start conversations with strangers; writing to their own owner is how an agent asks a question.
        if (initiator.IsBot && other.UserId != initiator.OwnerId)
            throw new ChatAccessDeniedException("A bot can start a direct chat only with its owner.");

        var now = DateTime.UtcNow;

        var chat = new Chat
        {
            Id = Guid.NewGuid(),
            Type = ChatType.Direct,
            DirectKey = DirectKeyFor(initiator.UserId, other.UserId),
            CreatedAt = now,
            UpdatedAt = now
        };

        chat.Join(ChatMember.Create(initiator.UserId, ChatRole.Member, now));
        chat.Join(ChatMember.Create(other.UserId, ChatRole.Member, now));

        return chat;
    }

    // The same for both orders of the pair, so a unique index allows one direct chat per pair.
    public static string DirectKeyFor(Guid first, Guid second)
    {
        return first.CompareTo(second) < 0
            ? $"{first}:{second}"
            : $"{second}:{first}";
    }

    public static Chat CreateGroup(
        ChatParticipant creator,
        ChatTitle title,
        IEnumerable<ChatParticipant> members)
    {
        if (creator.IsBot)
            throw new ChatAccessDeniedException("Bots cannot create group chats.");

        var now = DateTime.UtcNow;

        var chat = new Chat
        {
            Id = Guid.NewGuid(),
            Type = ChatType.Group,
            Title = title,
            CreatedAt = now,
            UpdatedAt = now
        };

        chat.Join(ChatMember.Create(creator.UserId, ChatRole.Owner, now));

        foreach (var member in members)
            if (!chat.IsMember(member.UserId))
                chat.Join(ChatMember.Create(member.UserId, ChatRole.Member, now));

        return chat;
    }

    public void AddMember(
        Guid requesterId,
        ChatParticipant participant)
    {
        EnsureGroup();

        var requester = GetMember(requesterId);

        if (requester.Role == ChatRole.Member)
            throw new ChatAccessDeniedException("Only the owner and admins can add members.");

        if (IsMember(participant.UserId))
            return;

        Join(ChatMember.Create(participant.UserId, ChatRole.Member, DateTime.UtcNow));
        UpdatedAt = DateTime.UtcNow;
    }

    // Removing yourself is leaving the chat.
    public void RemoveMember(
        Guid requesterId,
        Guid userId)
    {
        EnsureGroup();

        var requester = GetMember(requesterId);

        var target = _members.FirstOrDefault(x => x.UserId == userId)
                     ?? throw new ParticipantNotFoundException(userId);

        if (target.Role == ChatRole.Owner)
            throw new ChatAccessDeniedException("The owner cannot leave or be removed.");

        if (requesterId != userId && !Outranks(requester.Role, target.Role))
            throw new ChatAccessDeniedException("Not enough rights to remove this member.");

        _members.Remove(target);
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangeRole(
        Guid requesterId,
        Guid userId,
        ChatRole role)
    {
        EnsureGroup();

        var requester = GetMember(requesterId);

        if (requester.Role != ChatRole.Owner)
            throw new ChatAccessDeniedException("Only the owner can change roles.");

        if (role == ChatRole.Owner)
            throw new DomainException("Ownership cannot be transferred.");

        var target = _members.FirstOrDefault(x => x.UserId == userId)
                     ?? throw new ParticipantNotFoundException(userId);

        if (target.Role == ChatRole.Owner)
            throw new DomainException("The owner's role cannot be changed.");

        target.ChangeRole(role);
        UpdatedAt = DateTime.UtcNow;
    }

    // The sequence number is allocated by the repository before the call; the chat checks the rest.
    public Message PostMessage(
        Guid authorId,
        MessageContent content,
        long seq,
        Message? replyTo)
    {
        EnsureMember(authorId);

        if (replyTo != null && replyTo.ChatId != Id)
            throw new DomainException("A reply must refer to a message in the same chat.");

        return Message.Create(Id, seq, authorId, content, replyTo?.Seq);
    }

    // The last message number is kept by the repository, outside the chat document.
    public ReadMark MarkRead(
        Guid userId,
        long seq,
        long lastMessageSeq)
    {
        EnsureMember(userId);

        if (seq > lastMessageSeq)
            throw new DomainException($"Message #{seq} does not exist yet.");

        return new ReadMark(Id, userId, seq);
    }

    public void EnsureMember(Guid userId)
    {
        if (!IsMember(userId))
            throw new ChatNotFoundException(Id);
    }

    public bool IsMember(Guid userId)
    {
        return _members.Any(x => x.UserId == userId);
    }

    internal void IncrementVersion()
    {
        Version++;
    }

    private void Join(ChatMember member)
    {
        _members.Add(member);
        Raise(new ChatMemberJoined(Id, member.UserId));
    }

    private ChatMember GetMember(Guid userId)
    {
        return _members.FirstOrDefault(x => x.UserId == userId)
               ?? throw new ChatNotFoundException(Id);
    }

    private void EnsureGroup()
    {
        if (Type != ChatType.Group)
            throw new DomainException("Members can be managed only in group chats.");
    }

    // Lower enum value means more rights: Owner > Admin > Member.
    private static bool Outranks(ChatRole requester, ChatRole target)
    {
        return requester < target;
    }

    public Guid Id { get; private set; }

    public ChatType Type { get; private set; }

    // Group chats only; a direct chat is shown under the other member's name.
    public ChatTitle? Title { get; private set; }

    // Direct chats only.
    public string? DirectKey { get; private set; }

    public IReadOnlyList<ChatMember> Members => _members;

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    // Optimistic concurrency: two member changes at once must not overwrite each other.
    public int Version { get; private set; }
}
