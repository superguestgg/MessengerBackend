using Messenger.Chats.Domain;
using Messenger.Users.Contracts;

namespace Messenger.Chats.Application;

// Author names are not stored in messages; they are looked up in one batch per response.
public sealed class MessageResultBuilder
{
    private readonly IUsersApi _usersApi;
    private readonly ILiveLocationRepository _liveLocationRepository;
    private readonly TimeProvider _time;

    public MessageResultBuilder(
        IUsersApi usersApi,
        ILiveLocationRepository liveLocationRepository,
        TimeProvider time)
    {
        _usersApi = usersApi;
        _liveLocationRepository = liveLocationRepository;
        _time = time;
    }

    public async Task<IReadOnlyList<MessageResult>> Build(
        IReadOnlyList<Message> messages,
        CancellationToken cancellationToken)
    {
        if (messages.Count == 0)
            return [];

        var authors = (await _usersApi.GetAccounts(
                messages.Select(x => x.AuthorId).Distinct().ToArray(),
                cancellationToken))
            .ToDictionary(x => x.AccountId);

        // Live locations show their latest point, not the one the message was sent with.
        var liveIds = messages
            .Where(x => x.LiveUntil != null)
            .Select(x => x.Id)
            .ToArray();

        var live = liveIds.Length > 0
            ? (await _liveLocationRepository.GetByIds(liveIds, cancellationToken)).ToDictionary(x => x.Id)
            : new Dictionary<Guid, LiveLocation>();

        var now = _time.GetUtcNow().UtcDateTime;

        return messages
            .Select(x =>
            {
                var author = authors.GetValueOrDefault(x.AuthorId);

                return new MessageResult(
                    x.Id,
                    x.ChatId,
                    x.Seq,
                    x.AuthorId,
                    author?.DisplayName,
                    author?.IsBot ?? false,
                    x.Text?.Value,
                    x.Attachments.Select(BuildAttachment).ToArray(),
                    LocationResults.From(x, live.GetValueOrDefault(x.Id), now),
                    x.ReplyToSeq,
                    x.CreatedAt);
            })
            .ToArray();
    }

    private static AttachmentResult BuildAttachment(Attachment attachment)
    {
        return new AttachmentResult(
            attachment.FileId,
            attachment.Kind,
            attachment.FileName,
            attachment.ContentType,
            attachment.Size,
            attachment.DurationSeconds,
            attachment.Transcript != null
                ? new TranscriptResult(attachment.Transcript.Status, attachment.Transcript.Text)
                : null);
    }
}
