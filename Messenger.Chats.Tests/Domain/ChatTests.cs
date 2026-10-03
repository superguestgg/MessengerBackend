using Messenger.Chats.Domain;

namespace Messenger.Chats.Tests.Domain;

public class ChatTests
{
    private static ChatParticipant User() => new(Guid.NewGuid(), false, null);

    private static ChatParticipant BotOf(ChatParticipant owner) => new(Guid.NewGuid(), true, owner.UserId);

    private static Chat Group(ChatParticipant owner, params ChatParticipant[] members)
    {
        return Chat.CreateGroup(owner, new ChatTitle("Team"), members);
    }

    [Fact]
    public void CreateDirect_has_two_members_and_order_independent_key()
    {
        var alice = User();
        var bob = User();

        var chat = Chat.CreateDirect(alice, bob);

        Assert.Equal(ChatType.Direct, chat.Type);
        Assert.Equal(2, chat.Members.Count);
        Assert.All(chat.Members, x => Assert.Equal(ChatRole.Member, x.Role));
        Assert.Equal(Chat.DirectKeyFor(bob.UserId, alice.UserId), chat.DirectKey);
        Assert.Null(chat.Title);
    }

    [Fact]
    public void CreateDirect_with_self_is_rejected()
    {
        var alice = User();

        Assert.Throws<DomainException>(() => Chat.CreateDirect(alice, alice));
    }

    [Fact]
    public void Bot_starts_direct_chat_only_with_its_owner()
    {
        var owner = User();
        var bot = BotOf(owner);

        Chat.CreateDirect(bot, owner);

        Assert.Throws<ChatAccessDeniedException>(() => Chat.CreateDirect(bot, User()));
        Assert.Throws<ChatAccessDeniedException>(() => Chat.CreateDirect(bot, BotOf(owner)));
    }

    [Fact]
    public void User_can_start_direct_chat_with_any_bot()
    {
        var chat = Chat.CreateDirect(User(), BotOf(User()));

        Assert.Equal(2, chat.Members.Count);
    }

    [Fact]
    public void CreateGroup_makes_creator_owner_and_skips_duplicates()
    {
        var owner = User();
        var bob = User();

        var chat = Group(owner, bob, bob, owner);

        Assert.Equal(ChatType.Group, chat.Type);
        Assert.Equal("Team", chat.Title!.Value);
        Assert.Equal(2, chat.Members.Count);
        Assert.Equal(ChatRole.Owner, chat.Members.Single(x => x.UserId == owner.UserId).Role);
        Assert.Equal(ChatRole.Member, chat.Members.Single(x => x.UserId == bob.UserId).Role);
    }

    [Fact]
    public void Bots_cannot_create_groups()
    {
        Assert.Throws<ChatAccessDeniedException>(() => Group(BotOf(User())));
    }

    [Fact]
    public void Only_owner_and_admins_add_members()
    {
        var owner = User();
        var member = User();
        var chat = Group(owner, member);

        Assert.Throws<ChatAccessDeniedException>(() => chat.AddMember(member.UserId, User()));
        Assert.Throws<ChatNotFoundException>(() => chat.AddMember(Guid.NewGuid(), User()));

        chat.ChangeRole(owner.UserId, member.UserId, ChatRole.Admin);
        var newcomer = User();
        chat.AddMember(member.UserId, newcomer);

        Assert.True(chat.IsMember(newcomer.UserId));
    }

    [Fact]
    public void Adding_existing_member_is_a_no_op()
    {
        var owner = User();
        var member = User();
        var chat = Group(owner, member);

        chat.AddMember(owner.UserId, member);

        Assert.Equal(2, chat.Members.Count);
    }

    [Fact]
    public void Direct_chat_members_cannot_be_managed()
    {
        var alice = User();
        var chat = Chat.CreateDirect(alice, User());

        Assert.Throws<DomainException>(() => chat.AddMember(alice.UserId, User()));
        Assert.Throws<DomainException>(() => chat.RemoveMember(alice.UserId, alice.UserId));
    }

    [Fact]
    public void Removal_follows_ranks_and_owner_stays()
    {
        var owner = User();
        var admin = User();
        var member = User();
        var other = User();
        var chat = Group(owner, admin, member, other);
        chat.ChangeRole(owner.UserId, admin.UserId, ChatRole.Admin);

        Assert.Throws<ChatAccessDeniedException>(() => chat.RemoveMember(member.UserId, other.UserId));
        Assert.Throws<ChatAccessDeniedException>(() => chat.RemoveMember(member.UserId, admin.UserId));
        Assert.Throws<ChatAccessDeniedException>(() => chat.RemoveMember(admin.UserId, owner.UserId));
        Assert.Throws<ChatAccessDeniedException>(() => chat.RemoveMember(owner.UserId, owner.UserId));

        chat.RemoveMember(admin.UserId, other.UserId);
        chat.RemoveMember(member.UserId, member.UserId);
        chat.RemoveMember(owner.UserId, admin.UserId);

        Assert.Equal([owner.UserId], chat.Members.Select(x => x.UserId));
    }

    [Fact]
    public void Removing_non_member_is_reported()
    {
        var owner = User();
        var chat = Group(owner);

        Assert.Throws<ParticipantNotFoundException>(() => chat.RemoveMember(owner.UserId, Guid.NewGuid()));
    }

    [Fact]
    public void Only_owner_changes_roles_and_ownership_is_not_transferable()
    {
        var owner = User();
        var admin = User();
        var member = User();
        var chat = Group(owner, admin, member);
        chat.ChangeRole(owner.UserId, admin.UserId, ChatRole.Admin);

        Assert.Throws<ChatAccessDeniedException>(() => chat.ChangeRole(admin.UserId, member.UserId, ChatRole.Admin));
        Assert.Throws<DomainException>(() => chat.ChangeRole(owner.UserId, member.UserId, ChatRole.Owner));
        Assert.Throws<DomainException>(() => chat.ChangeRole(owner.UserId, owner.UserId, ChatRole.Member));
    }

    [Fact]
    public void PostMessage_requires_membership()
    {
        var alice = User();
        var chat = Chat.CreateDirect(alice, User());

        var message = chat.PostMessage(alice.UserId, new MessageText("hi"), 1, null);

        Assert.Equal(chat.Id, message.ChatId);
        Assert.Equal(1, message.Seq);
        Assert.Equal(alice.UserId, message.AuthorId);
        Assert.Null(message.ReplyToSeq);
        Assert.Throws<ChatNotFoundException>(() => chat.PostMessage(Guid.NewGuid(), new MessageText("hi"), 2, null));
    }

    [Fact]
    public void Reply_must_be_in_the_same_chat()
    {
        var alice = User();
        var chat = Chat.CreateDirect(alice, User());
        var other = Chat.CreateDirect(alice, User());
        var question = chat.PostMessage(alice.UserId, new MessageText("?"), 1, null);
        var foreign = other.PostMessage(alice.UserId, new MessageText("?"), 1, null);

        var reply = chat.PostMessage(alice.UserId, new MessageText("!"), 2, question);

        Assert.Equal(1, reply.ReplyToSeq);
        Assert.Throws<DomainException>(() => chat.PostMessage(alice.UserId, new MessageText("!"), 3, foreign));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" \n ")]
    public void MessageText_rejects_blank(string value)
    {
        Assert.Throws<DomainException>(() => new MessageText(value));
    }

    [Fact]
    public void MessageText_keeps_whitespace_and_rejects_too_long()
    {
        Assert.Equal("  indented\n", new MessageText("  indented\n").Value);
        Assert.Throws<DomainException>(() => new MessageText(new string('a', MessageText.MaxLength + 1)));
    }
}
