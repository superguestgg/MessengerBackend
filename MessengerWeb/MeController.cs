using Mediator;
using Messenger.Users.Application;
using Microsoft.AspNetCore.Mvc;

namespace MessengerWeb;
[ApiController]
[Route("api/me")]
public class MeController : ControllerBase
{
    private readonly IMediator _mediator;

    public MeController(IMediator mediator)
    {
        _mediator = mediator;
    }


    [HttpGet]
    public async Task<ActionResult<MeResult>> Get()
    {
        var result = await _mediator.Send(new GetMeQuery(User.GetAccountId()));

        return Ok(result);
    }
}
