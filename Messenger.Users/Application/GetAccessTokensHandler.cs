using Mediator;
using Messenger.Users.Domain;

namespace Messenger.Users.Application;

public sealed class GetAccessTokensHandler
    : IRequestHandler<GetAccessTokensQuery, IReadOnlyList<AccessTokenResult>>
{
    private readonly IAccessTokenRepository _tokenRepository;

    public GetAccessTokensHandler(IAccessTokenRepository tokenRepository)
    {
        _tokenRepository = tokenRepository;
    }


    public async ValueTask<IReadOnlyList<AccessTokenResult>> Handle(
        GetAccessTokensQuery request,
        CancellationToken cancellationToken)
    {
        var tokens = await _tokenRepository
            .GetActiveByAccount(request.AccountId, cancellationToken);

        return tokens
            .Select(x => new AccessTokenResult(
                x.Id,
                x.Name.Value,
                x.CreatedAt))
            .ToArray();
    }
}
