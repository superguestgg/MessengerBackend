using Mediator;
using Messenger.Chats.Domain;

namespace Messenger.Chats.Application;

public sealed class WaitForMessagesHandler
    : IRequestHandler<WaitForMessagesQuery, WaitForMessagesResult>
{
    public static readonly TimeSpan MaxTimeout = TimeSpan.FromSeconds(50);

    private const int ScanBatchSize = 100;

    // Signals cover this process only; the poll catches messages they missed.
    private static readonly TimeSpan FallbackPollInterval = TimeSpan.FromSeconds(5);

    private static readonly TimeSpan GapRecheckInterval = TimeSpan.FromMilliseconds(250);

    private readonly IChatRepository _chatRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly MessageResultBuilder _resultBuilder;
    private readonly IChatSignals _signals;
    private readonly WaitSlots _slots;
    private readonly TimeProvider _time;

    public WaitForMessagesHandler(
        IChatRepository chatRepository,
        IMessageRepository messageRepository,
        MessageResultBuilder resultBuilder,
        IChatSignals signals,
        WaitSlots slots,
        TimeProvider time)
    {
        _chatRepository = chatRepository;
        _messageRepository = messageRepository;
        _resultBuilder = resultBuilder;
        _signals = signals;
        _slots = slots;
        _time = time;
    }


    public async ValueTask<WaitForMessagesResult> Handle(
        WaitForMessagesQuery request,
        CancellationToken cancellationToken)
    {
        if (request.AfterSeq < 0)
            throw new DomainException("'after' must not be negative.");

        if (request.Timeout < TimeSpan.Zero || request.Timeout > MaxTimeout)
            throw new DomainException($"Timeout must be from 0 to {MaxTimeout.TotalSeconds} seconds.");

        var chat = await _chatRepository
            .Get(request.ChatId, cancellationToken);

        if (chat == null)
            throw new ChatNotFoundException(request.ChatId);

        chat.EnsureMember(request.UserId);

        using var slot = _slots.TryAcquire(request.UserId)
                         ?? throw new TooManyWaitsException();


        var deadline = _time.GetUtcNow() + request.Timeout;

        var cursor = request.AfterSeq;

        while (true)
        {
            var signal = _signals.Next(chat.Id);

            var page = await _messageRepository.GetPage(
                chat.Id,
                cursor,
                null,
                ScanBatchSize,
                cancellationToken);

            var (contiguous, nextAfter, blockedByGap) = ContiguousMessages
                .Take(page, cursor, _time.GetUtcNow().UtcDateTime);

            cursor = nextAfter;

            var matches = contiguous
                .Where(x => request.FromUserId == null || x.AuthorId == request.FromUserId)
                .Where(x => request.ReplyToSeq == null || x.ReplyToSeq == request.ReplyToSeq)
                .ToArray();

            if (matches.Length > 0)
                return new WaitForMessagesResult(
                    await _resultBuilder.Build(matches, cancellationToken),
                    cursor);

            // A full batch without matches: keep scanning before waiting.
            if (page.Count == ScanBatchSize && !blockedByGap)
                continue;

            var remaining = deadline - _time.GetUtcNow();

            if (remaining <= TimeSpan.Zero)
                return new WaitForMessagesResult([], cursor);

            var pause = blockedByGap ? GapRecheckInterval : FallbackPollInterval;

            await Task.WhenAny(
                signal,
                Task.Delay(remaining < pause ? remaining : pause, _time, cancellationToken));

            // The client went away: nobody reads the answer.
            if (cancellationToken.IsCancellationRequested)
                return new WaitForMessagesResult([], cursor);
        }
    }
}
