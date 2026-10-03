using Mediator;
using Messenger.Users.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MessengerWeb;
[ApiController]
[Route("api/account")]
public class AccountController : ControllerBase
{
    private readonly IMediator _mediator;

    public AccountController(IMediator mediator)
    {
        _mediator = mediator;
    }


    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<RegisterAccountResult>> Register(
        RegisterAccountCommand command)
    {
        var result = await _mediator.Send(command);

        return Ok(result);
    }
}
