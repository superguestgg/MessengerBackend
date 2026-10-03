using Mediator;
using Messenger.Users.Domain;

namespace Messenger.Users.Application;

// The query is an account id, an exact email or the start of a word in a display name.
public sealed record SearchUsersQuery(
    Guid RequesterId,
    string Query,
    int Limit
) : IRequest<IReadOnlyList<UserSearchResult>>
{
    public const int MinLength = 2;

    public const int MaxLength = Email.MaxLength;

    public const int MaxLimit = 20;

    public const int DefaultLimit = 10;
}
