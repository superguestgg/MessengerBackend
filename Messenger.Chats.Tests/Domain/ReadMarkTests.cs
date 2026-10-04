using Messenger.Chats.Domain;

namespace Messenger.Chats.Tests.Domain;

public class ReadMarkTests
{
    private static ChatParticipant User() => new(Guid.NewGuid(), false, null);

    private static Guid[] Joined(Chat chat)
    {
        return chat.DomainEvents
            .Select(x => Assert.IsType<ChatMemberJoined>(x))
            .Select(x =>
            {
                Assert.Equal(chat.Id, x.ChatId);
                return x.UserId;
            })
            .ToArray();
    }

    [Fact]
    public void Member_marks_read_up_to_the_last_message()
    {
        var alice = User();
        var chat = Chat.CreateDirect(alice, User());

        var mark = chat.MarkRead(alice.UserId, 5, 5);

        Assert.Equal((chat.Id, alice.UserId, 5L), (mark.ChatId, mark.UserId, mark.Seq));
        Assert.Equal(0, chat.MarkRead(alice.UserId, 0, 5).Seq);
    }

    [Fact]
    public void Message_that_does_not_exist_yet_cannot_be_marked_read()
    {
        var alice = User();
        var chat = Chat.CreateDirect(alice, User());

        Assert.Throws<DomainException>(() => chat.MarkRead(alice.UserId, 6, 5));
        Assert.Throws<DomainException>(() => chat.MarkRead(alice.UserId, 1, 0));
    }

    [Fact]
    public void Negative_mark_is_rejected()
    {
        var alice = User();
        var chat = Chat.CreateDirect(alice, User());

        Assert.Throws<DomainException>(() => chat.MarkRead(alice.UserId, -1, 5));
    }

    [Fact]
    public void Outsider_cannot_mark_read()
    {
        var chat = Chat.CreateDirect(User(), User());

        Assert.Throws<ChatNotFoundException>(() => chat.MarkRead(Guid.NewGuid(), 1, 5));
    }

    [Fact]
    public void Every_member_of_a_new_chat_joins()
    {
        var alice = User();
        var bob = User();
        var owner = User();

        var direct = Chat.CreateDirect(alice, bob);
        var group = Chat.CreateGroup(owner, new ChatTitle("Team"), [alice, bob, alice]);

        Assert.Equal([alice.UserId, bob.UserId], Joined(direct));
        Assert.Equal([owner.UserId, alice.UserId, bob.UserId], Joined(group));
    }

    [Fact]
    public void Added_member_joins_and_existing_one_does_not_join_again()
    {
        var owner = User();
        var member = User();
        var newcomer = User();
        var chat = Chat.CreateGroup(owner, new ChatTitle("Team"), [member]);
        chat.ClearDomainEvents();

        chat.AddMember(owner.UserId, member);
        chat.AddMember(owner.UserId, newcomer);

        Assert.Equal([newcomer.UserId], Joined(chat));
    }
}
