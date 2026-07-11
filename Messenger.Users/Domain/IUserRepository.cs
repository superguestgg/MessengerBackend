namespace Messenger.Users.Domain;

public interface IUserRepository
{
    Task<User?> Get(Guid id);

    Task<User?> GetByEmail(string email);

    Task Add(User user);

    Task Update(User user);
}