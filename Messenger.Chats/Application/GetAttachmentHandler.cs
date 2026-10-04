using Mediator;
using Messenger.Chats.Domain;
using Messenger.Files.Contracts;

namespace Messenger.Chats.Application;

// Access to a file follows the message: whoever can read the message can download what it carries.
public sealed class GetAttachmentHandler
    : IRequestHandler<GetAttachmentQuery, AttachmentContent>
{
    private readonly IChatRepository _chatRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IFilesApi _filesApi;

    public GetAttachmentHandler(
        IChatRepository chatRepository,
        IMessageRepository messageRepository,
        IFilesApi filesApi)
    {
        _chatRepository = chatRepository;
        _messageRepository = messageRepository;
        _filesApi = filesApi;
    }


    public async ValueTask<AttachmentContent> Handle(
        GetAttachmentQuery request,
        CancellationToken cancellationToken)
    {
        var chat = await _chatRepository
            .Get(request.ChatId, cancellationToken);

        if (chat == null)
            throw new ChatNotFoundException(request.ChatId);

        chat.EnsureMember(request.UserId);


        var message = await _messageRepository
            .GetBySeq(chat.Id, request.Seq, cancellationToken);

        var attachment = message?.Attachments.FirstOrDefault(x => x.FileId == request.FileId)
                         ?? throw new AttachmentNotFoundException(request.FileId);

        var content = await _filesApi.Open(attachment.FileId, cancellationToken)
                      ?? throw new AttachmentNotFoundException(request.FileId);


        return new AttachmentContent(
            content,
            attachment.FileName,
            attachment.ContentType,
            attachment.CanBeShownInline);
    }
}
