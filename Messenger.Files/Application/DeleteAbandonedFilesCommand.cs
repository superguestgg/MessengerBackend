using Mediator;

namespace Messenger.Files.Application;

// Removes uploads that were never attached to anything. Returns how many were removed.
public sealed record DeleteAbandonedFilesCommand : IRequest<int>;
