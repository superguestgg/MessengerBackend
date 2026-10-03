using System.ComponentModel.DataAnnotations;
using Mediator;
using Messenger.Users.Domain;

namespace Messenger.Users.Application;

public sealed record RegisterAccountCommand(
    [Required, EmailAddress, MaxLength(Email.MaxLength)]
    string Email,

    [Required, MinLength(8), MaxLength(128)]
    string Password
) : IRequest<RegisterAccountResult>;
