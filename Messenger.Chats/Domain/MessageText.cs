namespace Messenger.Chats.Domain;

public sealed record MessageText
{
    public const int MaxLength = 4096;

    // Not trimmed: leading spaces and line breaks can be part of the message.
    public MessageText(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > MaxLength)
            throw new DomainException(
                $"Message text must be 1 to {MaxLength} characters long and not blank.");

        Value = value;
    }

    public string Value { get; }

    public override string ToString() => Value;
}
