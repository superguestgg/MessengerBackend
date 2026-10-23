using Mediator;
using Messenger.Chats.Application;
using Microsoft.AspNetCore.Mvc;

namespace MessengerWeb;
[ApiController]
[Route("api/live-locations")]
public class LiveLocationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public LiveLocationsController(IMediator mediator)
    {
        _mediator = mediator;
    }


    // What the caller is broadcasting right now, in every chat, so any device can show and stop it.
    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyList<LiveLocationResult>>> Mine()
    {
        var result = await _mediator.Send(new GetMyLiveLocationsQuery(User.GetAccountId()));

        return Ok(result);
    }
}
