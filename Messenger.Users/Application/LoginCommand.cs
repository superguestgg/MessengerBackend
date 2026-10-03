using System.ComponentModel.DataAnnotations;
using Mediator;
using Messenger.Users.Domain;

namespace Messenger.Users.Application;

public sealed record LoginCommand(
    [Required, MaxLength(Email.MaxLength)]
    string Email,

    [Required, MaxLength(128)]
    string Password
) : IRequest<LoginResult>;
