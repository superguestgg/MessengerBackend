using Mediator;
using Messenger.Chats.Domain;

namespace Messenger.Chats.Application;

internal static class DomainEventPublishing
{
    // Call after the aggregate is saved, so handlers never see state that was rolled back.
    public static async ValueTask PublishDomainEvents(
        this IPublisher publisher,
        AggregateRoot aggregate,
        CancellationToken cancellationToken)
    {
        var events = aggregate.DomainEvents.ToArray();

        aggregate.ClearDomainEvents();

        foreach (var domainEvent in events)
            await publisher.Publish((object)domainEvent, cancellationToken);
    }
}
