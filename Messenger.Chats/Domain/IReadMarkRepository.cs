namespace Messenger.Chats.Domain;

public interface IReadMarkRepository
{
    // Keeps the larger of the stored and the new value: marks from several devices
    // may arrive out of order, and reading never moves back.
    Task Advance(ReadMark mark, CancellationToken cancellationToken = default);

    // Chat id → last read seq. Chats where the member has no mark are left out.
    Task<IReadOnlyDictionary<Guid, long>> GetForMember(
        Guid userId,
        IReadOnlyCollection<Guid> chatIds,
        CancellationToken cancellationToken = default);
}
