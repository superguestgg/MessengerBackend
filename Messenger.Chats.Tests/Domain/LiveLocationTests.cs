using Messenger.Chats.Domain;

namespace Messenger.Chats.Tests.Domain;

public class LiveLocationTests
{
    private static readonly GeoPoint Here = new(55.75, 37.61, 10);

    private static readonly GeoPoint There = new(59.93, 30.33, 5);

    private static ChatParticipant User() => new(Guid.NewGuid(), false, null);

    private static (Message Message, LiveLocation Live, Guid AuthorId, Guid OtherId) Start(TimeSpan period)
    {
        var alice = User();
        var bob = User();
        var chat = Chat.CreateDirect(alice, bob);
        var message = chat.PostMessage(alice.UserId, new MessageContent(null, [], Here, period), 3, null);

        return (message, LiveLocation.Start(message), alice.UserId, bob.UserId);
    }

    [Fact]
    public void Static_location_has_no_live_period()
    {
        var alice = User();
        var chat = Chat.CreateDirect(alice, User());

        var message = chat.PostMessage(alice.UserId, new MessageContent(null, [], Here, null), 1, null);

        Assert.Equal(Here, message.Location);
        Assert.Null(message.LiveUntil);
        Assert.Null(message.Text);
        Assert.Empty(message.Attachments);
        Assert.Throws<DomainException>(() => LiveLocation.Start(message));
    }

    [Fact]
    public void Location_message_carries_nothing_else()
    {
        Assert.Throws<DomainException>(() => new MessageContent(new MessageText("hi"), [], Here, null));
        Assert.Throws<DomainException>(() => new MessageContent(new MessageText("hi"), [], null, TimeSpan.FromHours(1)));
    }

    [Theory]
    [InlineData(LiveLocation.MinPeriodSeconds - 1)]
    [InlineData(LiveLocation.MaxPeriodSeconds + 1)]
    public void Live_period_outside_the_range_is_rejected(int seconds)
    {
        Assert.Throws<DomainException>(() => new MessageContent(null, [], Here, TimeSpan.FromSeconds(seconds)));
    }

    [Fact]
    public void Live_location_starts_at_the_first_point_and_ends_after_the_period()
    {
        var (message, live, authorId, _) = Start(TimeSpan.FromMinutes(15));

        Assert.Equal(message.CreatedAt.AddMinutes(15), message.LiveUntil);
        Assert.Equal(message.Id, live.Id);
        Assert.Equal(message.ChatId, live.ChatId);
        Assert.Equal(message.Seq, live.Seq);
        Assert.Equal(authorId, live.UserId);
        Assert.Equal(Here, live.Point);
        Assert.Equal(message.LiveUntil, live.ExpiresAt);
        Assert.True(live.IsActive(message.CreatedAt));
        Assert.False(live.IsActive(live.ExpiresAt));
    }

    [Fact]
    public void Author_moves_the_point()
    {
        var (_, live, authorId, _) = Start(TimeSpan.FromHours(1));
        var now = live.UpdatedAt.AddSeconds(10);

        Assert.True(live.Update(authorId, There, now));

        Assert.Equal(There, live.Point);
        Assert.Equal(now, live.UpdatedAt);
    }

    [Fact]
    public void Point_sent_too_soon_is_dropped()
    {
        var (_, live, authorId, _) = Start(TimeSpan.FromHours(1));
        var first = live.UpdatedAt;

        Assert.False(live.Update(authorId, There, first + LiveLocation.MinUpdateInterval / 2));

        Assert.Equal(Here, live.Point);
        Assert.Equal(first, live.UpdatedAt);
    }

    [Fact]
    public void Only_the_author_moves_or_stops_the_point()
    {
        var (_, live, _, otherId) = Start(TimeSpan.FromHours(1));
        var now = live.UpdatedAt.AddMinutes(1);

        Assert.Throws<ChatAccessDeniedException>(() => live.Update(otherId, There, now));
        Assert.Throws<ChatAccessDeniedException>(() => live.Stop(otherId, now));
        Assert.True(live.IsActive(now));
    }

    [Fact]
    public void Expired_location_rejects_new_points()
    {
        var (_, live, authorId, _) = Start(TimeSpan.FromMinutes(15));

        Assert.Throws<LiveLocationEndedException>(() => live.Update(authorId, There, live.ExpiresAt));
        Assert.Equal(Here, live.Point);
    }

    [Fact]
    public void Stopped_location_rejects_new_points_and_ends_when_stopped()
    {
        var (_, live, authorId, _) = Start(TimeSpan.FromHours(1));
        var stoppedAt = live.UpdatedAt.AddMinutes(5);

        Assert.True(live.Stop(authorId, stoppedAt));

        Assert.Equal(stoppedAt, live.StoppedAt);
        Assert.Equal(stoppedAt, live.EndsAt);
        Assert.False(live.IsActive(stoppedAt));
        Assert.Throws<LiveLocationEndedException>(() => live.Update(authorId, There, stoppedAt.AddMinutes(1)));
    }

    [Fact]
    public void Stopping_an_ended_location_changes_nothing()
    {
        var (_, live, authorId, _) = Start(TimeSpan.FromHours(1));
        var stoppedAt = live.UpdatedAt.AddMinutes(5);
        live.Stop(authorId, stoppedAt);

        Assert.False(live.Stop(authorId, stoppedAt.AddMinutes(1)));
        Assert.False(live.Stop(authorId, live.ExpiresAt.AddMinutes(1)));

        Assert.Equal(stoppedAt, live.StoppedAt);
    }

    [Fact]
    public void Expired_location_ends_at_the_planned_time()
    {
        var (_, live, authorId, _) = Start(TimeSpan.FromMinutes(15));

        Assert.False(live.Stop(authorId, live.ExpiresAt.AddMinutes(1)));

        Assert.Null(live.StoppedAt);
        Assert.Equal(live.ExpiresAt, live.EndsAt);
    }
}
