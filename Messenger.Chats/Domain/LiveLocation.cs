namespace Messenger.Chats.Domain;

// The latest point of a live location message. Kept apart from the message, which never
// changes, and only the last point is stored: there is no track of where the author went.
public class LiveLocation : AggregateRoot
{
    public const int MinPeriodSeconds = 60;

    public const int MaxPeriodSeconds = 8 * 60 * 60;

    // Points coming faster are dropped instead of being written to the database.
    public static readonly TimeSpan MinUpdateInterval = TimeSpan.FromSeconds(2);

    private LiveLocation()
    {
    }


    public static LiveLocation Start(Message message)
    {
        if (message.Location == null || message.LiveUntil == null)
            throw new DomainException("The message is not a live location.");

        return new LiveLocation
        {
            Id = message.Id,
            ChatId = message.ChatId,
            Seq = message.Seq,
            UserId = message.AuthorId,
            Point = message.Location,
            UpdatedAt = message.CreatedAt,
            ExpiresAt = message.LiveUntil.Value
        };
    }

    // False when the point came too soon after the previous one and was dropped.
    public bool Update(
        Guid userId,
        GeoPoint point,
        DateTime now)
    {
        EnsureAuthor(userId);

        if (!IsActive(now))
            throw new LiveLocationEndedException();

        if (now - UpdatedAt < MinUpdateInterval)
            return false;

        Point = point;
        UpdatedAt = now;

        return true;
    }

    // Stopping an ended broadcast changes nothing, so a second device can stop it too.
    public bool Stop(
        Guid userId,
        DateTime now)
    {
        EnsureAuthor(userId);

        if (!IsActive(now))
            return false;

        StoppedAt = now;

        return true;
    }

    public bool IsActive(DateTime now)
    {
        return StoppedAt == null && now < ExpiresAt;
    }

    private void EnsureAuthor(Guid userId)
    {
        if (userId != UserId)
            throw new ChatAccessDeniedException("Only the author can change a live location.");
    }

    // The id of the message.
    public Guid Id { get; private set; }

    public Guid ChatId { get; private set; }

    public long Seq { get; private set; }

    public Guid UserId { get; private set; }

    public GeoPoint Point { get; private set; } = null!;

    public DateTime UpdatedAt { get; private set; }

    public DateTime ExpiresAt { get; private set; }

    public DateTime? StoppedAt { get; private set; }

    public DateTime EndsAt => StoppedAt ?? ExpiresAt;
}
