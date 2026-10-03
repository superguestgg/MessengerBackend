namespace Messenger.Chats.Domain;

public sealed record ChatTitle
{
    public const int MaxLength = 128;

    public ChatTitle(string value)
    {
        var trimmed = value.Trim();

        if (trimmed.Length == 0 || trimmed.Length > MaxLength)
            throw new DomainException(
                $"Chat title must be 1 to {MaxLength} characters long.");

        Value = trimmed;
    }

    public string Value { get; }

    public override string ToString() => Value;
}
