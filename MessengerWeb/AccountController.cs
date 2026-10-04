using System.ComponentModel.DataAnnotations;
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

    [HttpPost("password")]
    public async Task<IActionResult> ChangePassword(
        ChangePasswordRequest request)
    {
        await _mediator.Send(new ChangePasswordCommand(
            User.GetAccountId(),
            request.CurrentPassword,
            request.NewPassword));

        return NoContent();
    }
}

public sealed record ChangePasswordRequest(
    [Required, MaxLength(128)]
    string CurrentPassword,

    [Required, MinLength(8), MaxLength(128)]
    string NewPassword
);
