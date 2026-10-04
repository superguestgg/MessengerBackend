using Mediator;
using Messenger.Files.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Messenger.Files.Infrastructure;

public sealed class AbandonedFilesCleanup : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _time;
    private readonly ILogger<AbandonedFilesCleanup> _logger;

    public AbandonedFilesCleanup(
        IServiceScopeFactory scopeFactory,
        TimeProvider time,
        ILogger<AbandonedFilesCleanup> logger)
    {
        _scopeFactory = scopeFactory;
        _time = time;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval, _time);

        do
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                var sender = scope.ServiceProvider.GetRequiredService<ISender>();

                var deleted = await sender.Send(new DeleteAbandonedFilesCommand(), stoppingToken);

                if (deleted > 0)
                    _logger.LogInformation("Deleted {Count} files that were never attached", deleted);
            }
            catch (Exception e) when (!stoppingToken.IsCancellationRequested)
            {
                // Next run retries; a failed cleanup must not stop the host.
                _logger.LogError(e, "Failed to delete abandoned files");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
