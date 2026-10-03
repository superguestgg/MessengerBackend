namespace Messenger.Users.Domain;

public sealed record DisplayName
{
    public const int MaxLength = 64;

    public DisplayName(string value)
    {
        var trimmed = value.Trim();

        if (trimmed.Length == 0 || trimmed.Length > MaxLength)
            throw new DomainException(
                $"Display name must be 1 to {MaxLength} characters long.");

        Value = trimmed;
    }

    public string Value { get; }

    public override string ToString() => Value;
}
