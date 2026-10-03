namespace Messenger.Users.Domain;

public sealed record Email
{
    public const int MaxLength = 254;

    public Email(string value)
    {
        var normalized = Normalize(value);

        if (!IsValid(normalized))
            throw new DomainException($"'{value}' is not a valid email.");

        Value = normalized;
    }

    // For input that may legitimately be something else, such as a search query.
    public static Email? TryCreate(string value)
    {
        return IsValid(Normalize(value)) ? new Email(value) : null;
    }

    private static string Normalize(string value) => value.Trim().ToLowerInvariant();

    private static bool IsValid(string normalized)
    {
        var at = normalized.IndexOf('@');

        return normalized.Length <= MaxLength
            && at > 0
            && at == normalized.LastIndexOf('@')
            && at != normalized.Length - 1;
    }

    public string Value { get; }

    public override string ToString() => Value;
}
