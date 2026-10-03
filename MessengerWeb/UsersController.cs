using System.ComponentModel.DataAnnotations;
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


    // query: an account id, an exact email or the start of a word in a display name.
    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyList<UserSearchResult>>> Search(
        [FromQuery, Required, MinLength(SearchUsersQuery.MinLength), MaxLength(SearchUsersQuery.MaxLength)] string query,
        [FromQuery, Range(1, SearchUsersQuery.MaxLimit)] int limit = SearchUsersQuery.DefaultLimit)
    {
        var result = await _mediator.Send(new SearchUsersQuery(
            User.GetAccountId(),
            query,
            limit));

        return Ok(result);
    }
}
