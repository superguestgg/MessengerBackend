namespace Messenger.Chats.Application;

// For a live location: the latest point, when it came, and when the broadcast ends or ended.
public sealed record LocationResult(
    double Latitude,
    double Longitude,
    double? AccuracyMeters,
    DateTime UpdatedAt,
    DateTime? LiveUntil,
    bool IsLive
);

public sealed record LiveLocationResult(
    Guid ChatId,
    long Seq,
    Guid UserId,
    LocationResult Location
);
