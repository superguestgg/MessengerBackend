using Messenger.Chats.Domain;
using MongoDB.Driver;

namespace Messenger.Chats.Infrastructure;

public sealed class LiveLocationRepository : ILiveLocationRepository
{
    public const string CollectionName = "chat_live_locations";

    private readonly IMongoCollection<LiveLocation> _liveLocations;

    public LiveLocationRepository(IMongoDatabase database)
    {
        _liveLocations = database.GetCollection<LiveLocation>(CollectionName);
    }

    public async Task<LiveLocation?> Get(Guid chatId, long seq, CancellationToken cancellationToken = default)
    {
        return await _liveLocations
            .Find(x => x.ChatId == chatId && x.Seq == seq)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LiveLocation>> GetByIds(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        return await _liveLocations
            .Find(Builders<LiveLocation>.Filter.In(x => x.Id, ids))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LiveLocation>> GetActiveInChat(
        Guid chatId,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        return await _liveLocations
            .Find(x => x.ChatId == chatId && x.StoppedAt == null && x.ExpiresAt > now)
            .SortBy(x => x.Seq)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LiveLocation>> GetActiveByUser(
        Guid userId,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        return await _liveLocations
            .Find(x => x.UserId == userId && x.StoppedAt == null && x.ExpiresAt > now)
            .ToListAsync(cancellationToken);
    }

    public Task Add(LiveLocation liveLocation, CancellationToken cancellationToken = default)
    {
        return _liveLocations.InsertOneAsync(liveLocation, cancellationToken: cancellationToken);
    }

    public async Task Update(LiveLocation liveLocation, CancellationToken cancellationToken = default)
    {
        var result = await _liveLocations.ReplaceOneAsync(
            x => x.Id == liveLocation.Id && x.StoppedAt == null,
            liveLocation,
            cancellationToken: cancellationToken);

        if (result.MatchedCount == 0)
            throw new LiveLocationEndedException();
    }
}
