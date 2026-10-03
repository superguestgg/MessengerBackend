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

    public Task<User?> Get(Guid id)
    {
        return _users
            .Find(x => x.Id == id)
            .FirstOrDefaultAsync();
    }

    public Task<User?> GetByEmail(string email)
    {
        return _users
            .Find(x => x.Email == email)
            .FirstOrDefaultAsync();
    }

    public Task Add(User user)
    {
        return _users.InsertOneAsync(user);
    }

    public Task Update(User user)
    {
        return _users.ReplaceOneAsync(
            x => x.Id == user.Id,
            user);
    }
}