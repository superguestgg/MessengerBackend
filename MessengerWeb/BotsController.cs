using System.ComponentModel.DataAnnotations;
using Mediator;
using Messenger.Users.Application;
using Messenger.Users.Domain;
using Microsoft.AspNetCore.Mvc;

namespace MessengerWeb;
[ApiController]
[Route("api/bots")]
public class BotsController : ControllerBase
{
    private readonly IMediator _mediator;

    public BotsController(IMediator mediator)
    {
        _mediator = mediator;
    }


    [HttpPost]
    public async Task<IActionResult> Create(
        CreateBotRequest request)
    {
        var result = await _mediator.Send(new CreateBotCommand(
            User.GetAccountId(),
            request.DisplayName));

        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var result = await _mediator.Send(new GetBotsQuery(User.GetAccountId()));

        return Ok(result);
    }

    [HttpPost("{botId:guid}/token")]
    public async Task<IActionResult> ReissueToken(Guid botId)
    {
        var result = await _mediator.Send(new ReissueBotTokenCommand(
            User.GetAccountId(),
            botId));

        return Ok(result);
    }

    [HttpDelete("{botId:guid}")]
    public async Task<IActionResult> Delete(Guid botId)
    {
        await _mediator.Send(new DeleteBotCommand(
            User.GetAccountId(),
            botId));

        return NoContent();
    }
}

public sealed record CreateBotRequest(
    [Required, MaxLength(DisplayName.MaxLength)]
    string DisplayName
);
