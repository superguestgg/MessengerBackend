using Mediator;
using Messenger.Chats.Domain;
using Messenger.Files.Contracts;

namespace Messenger.Chats.Application;

public sealed class SendMessageHandler
    : IRequestHandler<SendMessageCommand, SendMessageResult>
{
    private readonly IChatRepository _chatRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IFilesApi _filesApi;
    private readonly IPublisher _publisher;

    public SendMessageHandler(
        IChatRepository chatRepository,
        IMessageRepository messageRepository,
        IFilesApi filesApi,
        IPublisher publisher)
    {
        _chatRepository = chatRepository;
        _messageRepository = messageRepository;
        _filesApi = filesApi;
        _publisher = publisher;
    }


    public async ValueTask<SendMessageResult> Handle(
        SendMessageCommand request,
        CancellationToken cancellationToken)
    {
        var chat = await _chatRepository
            .Get(request.ChatId, cancellationToken);

        if (chat == null)
            throw new ChatNotFoundException(request.ChatId);

        // Checked before a number is reserved, so outsiders cannot burn sequence numbers.
        chat.EnsureMember(request.AuthorId);

        var text = request.Text != null
            ? new MessageText(request.Text)
            : null;

        var content = new MessageContent(
            text,
            await LoadAttachments(request, cancellationToken));

        Message? replyTo = null;

        if (request.ReplyToSeq != null)
            replyTo = await _messageRepository.GetBySeq(chat.Id, request.ReplyToSeq.Value, cancellationToken)
                      ?? throw new DomainException($"Message #{request.ReplyToSeq} was not found in this chat.");

        // Before the number is reserved: the message must not wait on other modules once it has one.
        await _filesApi.MarkAttached(
            content.Attachments.Select(x => x.FileId).ToArray(),
            cancellationToken);


        var seq = await _chatRepository
            .NextMessageSeq(chat.Id, DateTime.UtcNow, cancellationToken);

        var message = chat.PostMessage(request.AuthorId, content, seq, replyTo);


        await _messageRepository.Add(message, cancellationToken);

        await _publisher.PublishDomainEvents(message, cancellationToken);


        return new SendMessageResult(
            message.Id,
            message.ChatId,
            message.Seq,
            message.CreatedAt);
    }

    // Only the author's own uploads can be attached.
    private async Task<IReadOnlyList<Attachment>> LoadAttachments(
        SendMessageCommand request,
        CancellationToken cancellationToken)
    {
        var fileIds = request.Voice != null
            ? request.FileIds.Append(request.Voice.FileId).ToArray()
            : request.FileIds;

        if (fileIds.Count == 0)
            return [];

        var files = (await _filesApi.GetOwnFiles(request.AuthorId, fileIds, cancellationToken))
            .ToDictionary(x => x.FileId);

        FileDescription Find(Guid fileId) =>
            files.GetValueOrDefault(fileId) ?? throw new AttachmentNotFoundException(fileId);

        var attachments = request.FileIds
            .Select(Find)
            .Select(x => Attachment.File(x.FileId, x.FileName, x.ContentType, x.Size))
            .ToList();

        if (request.Voice != null)
        {
            var voice = Find(request.Voice.FileId);

            attachments.Add(Attachment.Voice(
                voice.FileId,
                voice.FileName,
                voice.ContentType,
                voice.Size,
                request.Voice.DurationSeconds));
        }

        return attachments;
    }
}
