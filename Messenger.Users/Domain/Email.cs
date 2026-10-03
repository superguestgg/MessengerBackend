namespace Messenger.Users.Domain;

public sealed record Email
{
    public const int MaxLength = 254;

    public Email(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();

        var at = normalized.IndexOf('@');

        if (normalized.Length > MaxLength
            || at <= 0
            || at != normalized.LastIndexOf('@')
            || at == normalized.Length - 1)
            throw new DomainException($"'{value}' is not a valid email.");

        Value = normalized;
    }

    public string Value { get; }

    public override string ToString() => Value;
}
