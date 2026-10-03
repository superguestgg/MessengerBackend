using Mediator;
using Messenger.Users.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MessengerWeb;
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }


    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResult>> Login(
        LoginCommand command)
    {
        var result = await _mediator.Send(command);

        return Ok(result);
    }
}
