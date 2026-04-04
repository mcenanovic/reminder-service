using ReminderService.Api.Services;

namespace ReminderService.Api.Workers;

public class ReminderWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReminderWorker> _logger;
    private readonly TimeSpan _pollingInterval;

    public ReminderWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<ReminderWorker> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        var seconds = configuration.GetValue("ReminderWorker:PollingIntervalSeconds", 30);
        _pollingInterval = TimeSpan.FromSeconds(seconds);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Reminder worker started. Polling every {Interval} seconds.", _pollingInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var reminderService = scope.ServiceProvider.GetRequiredService<IReminderService>();
                await reminderService.ProcessDueRemindersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing due reminders.");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }
    }
}
