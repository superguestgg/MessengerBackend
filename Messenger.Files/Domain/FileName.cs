namespace Messenger.Files.Domain;

public sealed record FileName
{
    public const int MaxLength = 255;

    // Some clients send a full local path: only the last segment is the name.
    public FileName(string value)
    {
        var name = value[(value.LastIndexOfAny(['/', '\\']) + 1)..];

        var cleaned = new string(name.Select(x => char.IsControl(x) ? '_' : x).ToArray()).Trim();

        if (cleaned.Length == 0 || cleaned.Length > MaxLength)
            throw new DomainException(
                $"File name must be 1 to {MaxLength} characters long.");

        Value = cleaned;
    }

    public string Value { get; }

    public override string ToString() => Value;
}
