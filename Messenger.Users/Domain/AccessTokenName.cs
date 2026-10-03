namespace Messenger.Users.Domain;

public sealed record AccessTokenName
{
    public const int MaxLength = 64;

    public AccessTokenName(string value)
    {
        var trimmed = value.Trim();

        if (trimmed.Length == 0 || trimmed.Length > MaxLength)
            throw new DomainException(
                $"Token name must be 1 to {MaxLength} characters long.");

        Value = trimmed;
    }

    public string Value { get; }

    public override string ToString() => Value;
}
