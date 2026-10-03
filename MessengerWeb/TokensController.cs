using System.ComponentModel.DataAnnotations;
using Mediator;
using Messenger.Users.Application;
using Messenger.Users.Domain;
using Microsoft.AspNetCore.Mvc;

namespace MessengerWeb;
[ApiController]
[Route("api/tokens")]
public class TokensController : ControllerBase
{
    private readonly IMediator _mediator;

    public TokensController(IMediator mediator)
    {
        _mediator = mediator;
    }


    [HttpPost]
    public async Task<ActionResult<IssuedAccessTokenResult>> Issue(
        IssueTokenRequest request)
    {
        var result = await _mediator.Send(new IssueAccessTokenCommand(
            User.GetAccountId(),
            request.Name));

        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AccessTokenResult>>> List()
    {
        var result = await _mediator.Send(new GetAccessTokensQuery(User.GetAccountId()));

        return Ok(result);
    }

    [HttpDelete("{tokenId:guid}")]
    public async Task<IActionResult> Revoke(Guid tokenId)
    {
        await _mediator.Send(new RevokeAccessTokenCommand(
            User.GetAccountId(),
            tokenId));

        return NoContent();
    }
}

public sealed record IssueTokenRequest(
    [Required, MaxLength(AccessTokenName.MaxLength)]
    string Name
);
