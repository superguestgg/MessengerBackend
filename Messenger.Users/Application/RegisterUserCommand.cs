using System.ComponentModel.DataAnnotations;
using Mediator;

namespace Messenger.Users.Application;

public sealed record RegisterUserCommand(
    [Required, EmailAddress, MaxLength(254)]
    string Email,

    [Required, MinLength(8), MaxLength(128)]
    string Password,

    [Required, MaxLength(64)]
    string DisplayName
) : IRequest<RegisterUserResult>;
