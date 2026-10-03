using Mediator;
using Messenger.Users.Domain;

namespace Messenger.Users.Application;

public sealed class RevokeAccessTokenHandler
    : IRequestHandler<RevokeAccessTokenCommand>
{
    private readonly IAccessTokenRepository _tokenRepository;

    public RevokeAccessTokenHandler(IAccessTokenRepository tokenRepository)
    {
        _tokenRepository = tokenRepository;
    }


    public async ValueTask<Unit> Handle(
        RevokeAccessTokenCommand request,
        CancellationToken cancellationToken)
    {
        var token = await _tokenRepository
            .Get(request.TokenId, cancellationToken);

        if (token == null)
            throw new AccessTokenNotFoundException(request.TokenId);


        token.Revoke(request.RequesterId);


        await _tokenRepository.Update(token, cancellationToken);


        return Unit.Value;
    }
}
