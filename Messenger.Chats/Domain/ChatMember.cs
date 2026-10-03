namespace Messenger.Chats.Domain;

public class ChatMember
{
    private ChatMember()
    {
    }


    internal static ChatMember Create(
        Guid userId,
        ChatRole role,
        DateTime joinedAt)
    {
        return new ChatMember
        {
            UserId = userId,
            Role = role,
            JoinedAt = joinedAt
        };
    }

    internal void ChangeRole(ChatRole role)
    {
        Role = role;
    }

    public Guid UserId { get; private set; }

    public ChatRole Role { get; private set; }

    public DateTime JoinedAt { get; private set; }
}
