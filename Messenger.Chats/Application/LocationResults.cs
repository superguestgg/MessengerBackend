using Messenger.Chats.Domain;

namespace Messenger.Chats.Application;

internal static class LocationResults
{
    public static LocationResult From(
        LiveLocation live,
        DateTime now)
    {
        return new LocationResult(
            live.Point.Latitude,
            live.Point.Longitude,
            live.Point.AccuracyMeters,
            live.UpdatedAt,
            live.EndsAt,
            live.IsActive(now));
    }

    public static LiveLocationResult ToLiveResult(
        LiveLocation live,
        DateTime now)
    {
        return new LiveLocationResult(
            live.ChatId,
            live.Seq,
            live.UserId,
            From(live, now));
    }

    // The live part is missing only if saving it failed right after the message.
    public static LocationResult? From(
        Message message,
        LiveLocation? live,
        DateTime now)
    {
        if (live != null)
            return From(live, now);

        if (message.Location == null)
            return null;

        return new LocationResult(
            message.Location.Latitude,
            message.Location.Longitude,
            message.Location.AccuracyMeters,
            message.CreatedAt,
            message.LiveUntil,
            false);
    }
}
