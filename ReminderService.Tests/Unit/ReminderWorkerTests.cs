using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ReminderService.Api.Services;
using ReminderService.Api.Workers;

namespace ReminderService.Tests.Unit;

public class ReminderWorkerTests
{
    private readonly IReminderService _reminderService;
    private readonly ILogger<ReminderWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public ReminderWorkerTests()
    {
        _reminderService = Substitute.For<IReminderService>();
        _logger = Substitute.For<ILogger<ReminderWorker>>();
        _scopeFactory = Substitute.For<IServiceScopeFactory>();

        var scope = Substitute.For<IServiceScope>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IReminderService)).Returns(_reminderService);
        scope.ServiceProvider.Returns(serviceProvider);
        _scopeFactory.CreateScope().Returns(scope);
    }

    private ReminderWorker CreateWorker(int pollingIntervalSeconds = 1)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ReminderWorker:PollingIntervalSeconds"] = pollingIntervalSeconds.ToString()
            })
            .Build();

        return new ReminderWorker(_scopeFactory, _logger, configuration);
    }

    [Fact]
    public async Task ExecuteAsync_ProcessesDueReminders()
    {
        var worker = CreateWorker();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        await worker.StartAsync(cts.Token);
        await Task.Delay(1500, cts.Token);
        await worker.StopAsync(CancellationToken.None);

        await _reminderService.Received().ProcessDueRemindersAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenServiceThrows_ContinuesPolling()
    {
        _reminderService.ProcessDueRemindersAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new Exception("DB error"));

        var worker = CreateWorker();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

        await worker.StartAsync(cts.Token);
        await Task.Delay(2500, cts.Token);
        await worker.StopAsync(CancellationToken.None);

        // Should have been called more than once despite throwing each time
        var callCount = _reminderService.ReceivedCalls().Count(c => c.GetMethodInfo().Name == "ProcessDueRemindersAsync");
        Assert.True(callCount >= 2, $"Expected at least 2 calls but received {callCount}");
    }

    [Fact]
    public async Task ExecuteAsync_ReadsPollingIntervalFromConfiguration()
    {
        // With a 5-second interval and only 1.5s of runtime, the worker should
        // process exactly once (the immediate first run) and not poll again
        var worker = CreateWorker(pollingIntervalSeconds: 5);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        await worker.StartAsync(cts.Token);
        await Task.Delay(1500, cts.Token);
        await worker.StopAsync(CancellationToken.None);

        var callCount = _reminderService.ReceivedCalls().Count(c => c.GetMethodInfo().Name == "ProcessDueRemindersAsync");
        Assert.Equal(1, callCount);
    }
}
