using System.Text.RegularExpressions;

namespace Messenger.Files.Domain;

// "type/subtype" in lower case, without parameters: "audio/webm;codecs=opus" becomes "audio/webm".
public sealed partial record MediaType
{
    public const int MaxLength = 255;

    public static readonly MediaType Unknown = new("application/octet-stream");

    public MediaType(string value)
    {
        var separator = value.IndexOf(';');

        var normalized = (separator >= 0 ? value[..separator] : value)
            .Trim()
            .ToLowerInvariant();

        if (normalized.Length > MaxLength || !Pattern().IsMatch(normalized))
            throw new DomainException($"'{value}' is not a valid media type.");

        Value = normalized;
    }

    // Browsers send no type for files they don't recognize.
    public static MediaType OrUnknown(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? Unknown
            : new MediaType(value);
    }

    public string Value { get; }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[a-z0-9][a-z0-9!#$&^_.+-]*/[a-z0-9][a-z0-9!#$&^_.+-]*$")]
    private static partial Regex Pattern();
}
