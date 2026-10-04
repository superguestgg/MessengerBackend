using Mediator;

namespace Messenger.Chats.Application;

public sealed record GetAttachmentQuery(
    Guid UserId,
    Guid ChatId,
    long Seq,
    Guid FileId
) : IRequest<AttachmentContent>;
