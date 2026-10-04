namespace Messenger.Chats.Application;

// The caller disposes Content.
public sealed record AttachmentContent(
    Stream Content,
    string FileName,
    string ContentType,
    bool CanBeShownInline
);
