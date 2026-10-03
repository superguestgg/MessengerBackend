using Messenger.Users.Domain;
using MongoDB.Driver;

namespace Messenger.Users.Infrastructure;

public class UserRepository : IUserRepository
{
    private readonly IMongoCollection<User> _users;

    public UserRepository(IMongoDatabase database)
    {
        _users = database.GetCollection<User>("users");
    }

    public async Task<User?> Get(Guid id)
    {
        return await _users
            .Find(x => x.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<User?> GetByEmail(string email)
    {
        return await _users
            .Find(x => x.Email == email)
            .FirstOrDefaultAsync();
    }

    public async Task Add(User user)
    {
        try
        {
            await _users.InsertOneAsync(user);
        }
        catch (MongoWriteException e)
            when (e.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            // Two concurrent registrations passed the GetByEmail check; the unique index caught it.
            throw new EmailAlreadyTakenException(user.Email);
        }
    }

    public Task Update(User user)
    {
        return _users.ReplaceOneAsync(
            x => x.Id == user.Id,
            user);
    }
}