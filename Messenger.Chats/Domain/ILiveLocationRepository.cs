namespace Messenger.Chats.Domain;

public interface ILiveLocationRepository
{
    Task<LiveLocation?> Get(Guid chatId, long seq, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LiveLocation>> GetByIds(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LiveLocation>> GetActiveInChat(Guid chatId, DateTime now, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LiveLocation>> GetActiveByUser(Guid userId, DateTime now, CancellationToken cancellationToken = default);

    Task Add(LiveLocation liveLocation, CancellationToken cancellationToken = default);

    // Throws LiveLocationEndedException if the broadcast was stopped since it was loaded,
    // so a late point from one device cannot undo a stop from another.
    Task Update(LiveLocation liveLocation, CancellationToken cancellationToken = default);
}
