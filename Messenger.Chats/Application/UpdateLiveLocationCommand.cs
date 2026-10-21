using Mediator;

namespace Messenger.Chats.Application;

public sealed record UpdateLiveLocationCommand(
    Guid UserId,
    Guid ChatId,
    long Seq,
    double Latitude,
    double Longitude,
    double? AccuracyMeters
) : IRequest;
