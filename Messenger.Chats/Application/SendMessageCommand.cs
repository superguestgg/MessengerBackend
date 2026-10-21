using Mediator;

namespace Messenger.Chats.Application;

// Files are uploaded to the Files module first; the message refers to them by id.
public sealed record SendMessageCommand(
    Guid AuthorId,
    Guid ChatId,
    string? Text,
    IReadOnlyList<Guid> FileIds,
    SendMessageVoice? Voice,
    SendMessageLocation? Location,
    long? ReplyToSeq
) : IRequest<SendMessageResult>;

public sealed record SendMessageVoice(
    Guid FileId,
    int DurationSeconds
);

// Live when LivePeriodSeconds is set.
public sealed record SendMessageLocation(
    double Latitude,
    double Longitude,
    double? AccuracyMeters,
    int? LivePeriodSeconds
);
