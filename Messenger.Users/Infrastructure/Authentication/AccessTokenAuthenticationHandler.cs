using System.Security.Claims;
using System.Text.Encodings.Web;
using Mediator;
using Messenger.Users.Application;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Messenger.Users.Infrastructure.Authentication;

public sealed class AccessTokenAuthenticationHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IMediator _mediator;

    public AccessTokenAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IMediator mediator)
        : base(options, logger, encoder)
    {
        _mediator = mediator;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var token = MessengerAuthentication.GetBearerToken(Request.Headers[HeaderNames.Authorization]);

        if (token == null)
            return AuthenticateResult.NoResult();

        var account = await _mediator.Send(
            new AuthenticateAccessTokenQuery(token),
            Context.RequestAborted);

        if (account == null)
            return AuthenticateResult.Fail("Invalid access token.");

        var identity = new ClaimsIdentity(
            [
                new Claim(AccountClaims.AccountId, account.AccountId.ToString()),
                new Claim(AccountClaims.AccountType, account.Type.ToString())
            ],
            Scheme.Name,
            AccountClaims.AccountId,
            null);

        return AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }
}
