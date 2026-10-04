using Messenger.Chats.Domain;

namespace Messenger.Chats.Tests.Domain;

public class MessageTests
{
    private static ChatParticipant User() => new(Guid.NewGuid(), false, null);

    private static MessageContent Text(string value) => new(new MessageText(value), []);

    [Fact]
    public void Message_keeps_text_as_written_and_is_stamped_in_utc()
    {
        var alice = User();
        var chat = Chat.CreateDirect(alice, User());
        var before = DateTime.UtcNow;

        var message = chat.PostMessage(alice.UserId, Text("  line 1\nline 2\n"), 7, null);

        Assert.Equal("  line 1\nline 2\n", message.Text?.Value);
        Assert.Equal(7, message.Seq);
        Assert.Equal(DateTimeKind.Utc, message.CreatedAt.Kind);
        Assert.InRange(message.CreatedAt, before, DateTime.UtcNow);
        Assert.NotEqual(Guid.Empty, message.Id);
    }

    [Fact]
    public void Every_message_gets_its_own_id()
    {
        var alice = User();
        var chat = Chat.CreateDirect(alice, User());

        var first = chat.PostMessage(alice.UserId, Text("a"), 1, null);
        var second = chat.PostMessage(alice.UserId, Text("b"), 2, null);

        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public void Reply_stores_the_seq_of_the_replied_message()
    {
        var alice = User();
        var bob = User();
        var chat = Chat.CreateDirect(alice, bob);
        var question = chat.PostMessage(alice.UserId, Text("?"), 41, null);

        var answer = chat.PostMessage(bob.UserId, Text("!"), 42, question);

        Assert.Equal(41, answer.ReplyToSeq);
        Assert.Equal(bob.UserId, answer.AuthorId);
    }

    [Fact]
    public void Reply_to_own_message_is_allowed()
    {
        var alice = User();
        var chat = Chat.CreateDirect(alice, User());
        var first = chat.PostMessage(alice.UserId, Text("one"), 1, null);

        var followUp = chat.PostMessage(alice.UserId, Text("two"), 2, first);

        Assert.Equal(1, followUp.ReplyToSeq);
    }

    [Fact]
    public void Removed_member_cannot_post()
    {
        var owner = User();
        var member = User();
        var chat = Chat.CreateGroup(owner, new ChatTitle("Team"), [member]);

        chat.RemoveMember(owner.UserId, member.UserId);

        Assert.Throws<ChatNotFoundException>(() => chat.PostMessage(member.UserId, Text("hi"), 1, null));
    }

    [Fact]
    public void MessageText_up_to_max_length_is_accepted()
    {
        var value = new string('a', MessageText.MaxLength);

        Assert.Equal(value, new MessageText(value).Value);
    }
}
