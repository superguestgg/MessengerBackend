using System.ComponentModel.DataAnnotations;

namespace Messenger.Users.Infrastructure.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; set; } = null!;

    [Required]
    public string Audience { get; set; } = null!;

    // HMAC-SHA256 needs at least 256 bits of key.
    [Required, MinLength(32)]
    public string SigningKey { get; set; } = null!;

    [Range(1, 24 * 60)]
    public int LifetimeMinutes { get; set; } = 60;
}
