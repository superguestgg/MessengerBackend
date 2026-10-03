using System.ComponentModel.DataAnnotations;
using Mediator;
using Messenger.Users.Application;
using Messenger.Users.Domain;
using Microsoft.AspNetCore.Mvc;

namespace MessengerWeb;
[ApiController]
[Route("api/profiles")]
public class ProfilesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProfilesController(IMediator mediator)
    {
        _mediator = mediator;
    }


    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> Get(Guid userId)
    {
        var profile = await _mediator.Send(new GetProfileQuery(userId));

        return profile == null ? NotFound() : Ok(profile);
    }

    // userId is the caller or one of the caller's bots.
    [HttpPut("{userId:guid}")]
    public async Task<IActionResult> Update(
        Guid userId,
        UpdateProfileRequest request)
    {
        await _mediator.Send(new UpdateProfileCommand(
            User.GetAccountId(),
            userId,
            request.DisplayName,
            request.Bio));

        return NoContent();
    }
}

public sealed record UpdateProfileRequest(
    [Required, MaxLength(DisplayName.MaxLength)]
    string DisplayName,

    [MaxLength(UserProfile.BioMaxLength)]
    string? Bio
);
