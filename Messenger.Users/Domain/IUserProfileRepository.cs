namespace Messenger.Users.Domain;

public interface IUserProfileRepository
{
    Task<UserProfile?> Get(Guid userId);

    Task Add(UserProfile profile);

    Task Update(UserProfile profile);
}