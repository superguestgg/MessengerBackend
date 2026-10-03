using Mediator;
using Messenger.Users.Application;
using Microsoft.AspNetCore.Mvc;

namespace MessengerWeb;
[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }


    [HttpPost("register")]
    public async Task<IActionResult> Register(
        RegisterUserCommand command)
    {
        var result = await _mediator.Send(command);

        return Ok(result);
    }
}