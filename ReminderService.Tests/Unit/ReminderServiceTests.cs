using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ReminderService.Api.DTOs;
using ReminderService.Api.Models;
using ReminderService.Api.Repositories;
using ReminderService.Api.Services;

namespace ReminderService.Tests.Unit;

public class ReminderServiceTests
{
    private readonly IReminderRepository _repository;
    private readonly Api.Services.ReminderService _reminderService;

    public ReminderServiceTests()
    {
        _repository = Substitute.For<IReminderRepository>();
        _reminderService = new Api.Services.ReminderService(
            _repository,
            Substitute.For<ILogger<Api.Services.ReminderService>>());
    }

    [Fact]
    public async Task CreateReminderAsync_WithValidRequest_ReturnsScheduledReminder()
    {
        var request = new CreateReminderRequest(
            Message: "Test reminder",
            SendAt: DateTimeOffset.UtcNow.AddHours(1),
            Email: "test@example.com"
        );

        _repository.CreateAsync(Arg.Any<Reminder>(), Arg.Any<CancellationToken>()).Returns(callInfo => callInfo.Arg<Reminder>());

        var result = await _reminderService.CreateReminderAsync(request);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Test reminder", result.Message);
        Assert.Equal(ReminderStatus.Scheduled, result.Status);
        Assert.Equal("test@example.com", result.Email);
        await _repository.Received(1).CreateAsync(Arg.Any<Reminder>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateReminderAsync_WithPastSendAt_ThrowsArgumentException()
    {
        var request = new CreateReminderRequest(
            Message: "Late reminder",
            SendAt: DateTimeOffset.UtcNow.AddHours(-1)
        );

        await Assert.ThrowsAsync<ArgumentException>(() => _reminderService.CreateReminderAsync(request));

        await _repository.DidNotReceive().CreateAsync(Arg.Any<Reminder>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateReminderAsync_WithoutEmail_ReturnsReminderWithNullEmail()
    {
        var request = new CreateReminderRequest(
            Message: "No email reminder",
            SendAt: DateTimeOffset.UtcNow.AddHours(1)
        );

        _repository.CreateAsync(Arg.Any<Reminder>(), Arg.Any<CancellationToken>()).Returns(callInfo => callInfo.Arg<Reminder>());

        var result = await _reminderService.CreateReminderAsync(request);

        Assert.Null(result.Email);
        Assert.Equal(ReminderStatus.Scheduled, result.Status);
    }

    [Fact]
    public async Task CreateReminderAsync_WithEmptyEmail_ReturnsReminderWithNullEmail()
    {
        var request = new CreateReminderRequest(
            Message: "Empty email reminder",
            SendAt: DateTimeOffset.UtcNow.AddHours(1),
            Email: ""
        );

        _repository.CreateAsync(Arg.Any<Reminder>(), Arg.Any<CancellationToken>()).Returns(callInfo => callInfo.Arg<Reminder>());

        var result = await _reminderService.CreateReminderAsync(request);

        Assert.Null(result.Email);
    }

    [Fact]
    public async Task GetAllRemindersAsync_ReturnsAllReminders()
    {
        var reminders = new List<Reminder>
        {
            new() { Id = Guid.NewGuid(), Message = "First", Status = ReminderStatus.Scheduled },
            new() { Id = Guid.NewGuid(), Message = "Second", Status = ReminderStatus.Sent }
        };
        _repository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(reminders);

        var result = await _reminderService.GetAllRemindersAsync();

        Assert.Equal(2, result.Count);
        await _repository.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateReminderAsync_WithSendAtExactlyNow_ThrowsArgumentException()
    {
        var request = new CreateReminderRequest(
            Message: "Exact now reminder",
            SendAt: DateTimeOffset.UtcNow
        );

        await Assert.ThrowsAsync<ArgumentException>(() => _reminderService.CreateReminderAsync(request));

        await _repository.DidNotReceive().CreateAsync(Arg.Any<Reminder>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateReminderAsync_WithFutureLocalTimeButPastUtc_ThrowsArgumentException()
    {
        // 14:30 at +03:00 is 11:30 UTC — if it's currently 12:00 UTC, this is in the past
        var pastInUtc = DateTimeOffset.UtcNow.AddHours(-1);
        var futureInLocalTz = pastInUtc.ToOffset(TimeSpan.FromHours(3));

        var request = new CreateReminderRequest(
            Message: "Looks future but is past in UTC",
            SendAt: futureInLocalTz
        );

        await Assert.ThrowsAsync<ArgumentException>(() => _reminderService.CreateReminderAsync(request));

        await _repository.DidNotReceive().CreateAsync(Arg.Any<Reminder>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessDueRemindersAsync_WithDueReminders_MarksEachAsSent()
    {
        var reminders = new List<Reminder>
        {
            new() { Id = Guid.NewGuid(), Message = "Due 1" },
            new() { Id = Guid.NewGuid(), Message = "Due 2" }
        };
        _repository.GetDueRemindersAsync(Arg.Any<CancellationToken>()).Returns(reminders);

        var before = DateTimeOffset.UtcNow;
        await _reminderService.ProcessDueRemindersAsync();
        var after = DateTimeOffset.UtcNow;

        await _repository.Received(1).GetDueRemindersAsync(Arg.Any<CancellationToken>());
        await _repository.Received(1).MarkAsSentAsync(reminders[0].Id, Arg.Is<DateTimeOffset>(dt => dt >= before && dt <= after), Arg.Any<CancellationToken>());
        await _repository.Received(1).MarkAsSentAsync(reminders[1].Id, Arg.Is<DateTimeOffset>(dt => dt >= before && dt <= after), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessDueRemindersAsync_WithNoDueReminders_DoesNotMarkAnything()
    {
        _repository.GetDueRemindersAsync(Arg.Any<CancellationToken>()).Returns(new List<Reminder>());

        await _reminderService.ProcessDueRemindersAsync();

        await _repository.Received(1).GetDueRemindersAsync(Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().MarkAsSentAsync(Arg.Any<Guid>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessDueRemindersAsync_WhenReminderFails_IncrementsRetryCount()
    {
        var reminder = new Reminder { Id = Guid.NewGuid(), Message = "Failing", RetryCount = 0 };
        _repository.GetDueRemindersAsync(Arg.Any<CancellationToken>()).Returns(new List<Reminder> { reminder });
        _repository.MarkAsSentAsync(reminder.Id, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).ThrowsAsync(new Exception("DB error"));

        await _reminderService.ProcessDueRemindersAsync();

        await _repository.Received(1).IncrementRetryCountAsync(reminder.Id, Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().MarkAsFailedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessDueRemindersAsync_WhenMaxRetriesReached_MarksAsFailed()
    {
        var reminder = new Reminder { Id = Guid.NewGuid(), Message = "Persistent failure", RetryCount = 2 };
        _repository.GetDueRemindersAsync(Arg.Any<CancellationToken>()).Returns(new List<Reminder> { reminder });
        _repository.MarkAsSentAsync(reminder.Id, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).ThrowsAsync(new Exception("DB error"));

        await _reminderService.ProcessDueRemindersAsync();

        await _repository.Received(1).IncrementRetryCountAsync(reminder.Id, Arg.Any<CancellationToken>());
        await _repository.Received(1).MarkAsFailedAsync(reminder.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessDueRemindersAsync_WhenOneFailsOthersContinue()
    {
        var failing = new Reminder { Id = Guid.NewGuid(), Message = "Failing" };
        var succeeding = new Reminder { Id = Guid.NewGuid(), Message = "Succeeding" };
        _repository.GetDueRemindersAsync(Arg.Any<CancellationToken>()).Returns(new List<Reminder> { failing, succeeding });
        _repository.MarkAsSentAsync(failing.Id, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).ThrowsAsync(new Exception("DB error"));

        await _reminderService.ProcessDueRemindersAsync();

        // Failing reminder gets retry increment
        await _repository.Received(1).IncrementRetryCountAsync(failing.Id, Arg.Any<CancellationToken>());
        // Succeeding reminder still gets marked as sent
        await _repository.Received(1).MarkAsSentAsync(succeeding.Id, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessDueRemindersAsync_CallsDeliveryService()
    {
        var deliveryService = Substitute.For<IReminderDeliveryService>();
        var serviceWithDelivery = new Api.Services.ReminderService(
            _repository,
            Substitute.For<ILogger<Api.Services.ReminderService>>(),
            deliveryService);

        var reminder = new Reminder { Id = Guid.NewGuid(), Message = "Test" };
        _repository.GetDueRemindersAsync(Arg.Any<CancellationToken>()).Returns(new List<Reminder> { reminder });

        await serviceWithDelivery.ProcessDueRemindersAsync();

        await deliveryService.Received(1).DeliverAsync(reminder);
    }

    [Fact]
    public async Task ProcessDueRemindersAsync_WhenDeliveryFails_IncrementsRetryCount()
    {
        var deliveryService = Substitute.For<IReminderDeliveryService>();
        var serviceWithDelivery = new Api.Services.ReminderService(
            _repository,
            Substitute.For<ILogger<Api.Services.ReminderService>>(),
            deliveryService);

        var reminder = new Reminder { Id = Guid.NewGuid(), Message = "Delivery failure", RetryCount = 0 };
        _repository.GetDueRemindersAsync(Arg.Any<CancellationToken>()).Returns(new List<Reminder> { reminder });
        deliveryService.DeliverAsync(reminder).ThrowsAsync(new Exception("Email service down"));

        await serviceWithDelivery.ProcessDueRemindersAsync();

        await _repository.Received(1).IncrementRetryCountAsync(reminder.Id, Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().MarkAsSentAsync(Arg.Any<Guid>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessDueRemindersAsync_WhenDeliveryFailsAtMaxRetries_MarksAsFailed()
    {
        var deliveryService = Substitute.For<IReminderDeliveryService>();
        var serviceWithDelivery = new Api.Services.ReminderService(
            _repository,
            Substitute.For<ILogger<Api.Services.ReminderService>>(),
            deliveryService);

        var reminder = new Reminder { Id = Guid.NewGuid(), Message = "Persistent delivery failure", RetryCount = 2 };
        _repository.GetDueRemindersAsync(Arg.Any<CancellationToken>()).Returns(new List<Reminder> { reminder });
        deliveryService.DeliverAsync(reminder).ThrowsAsync(new Exception("Email service down"));

        await serviceWithDelivery.ProcessDueRemindersAsync();

        await _repository.Received(1).IncrementRetryCountAsync(reminder.Id, Arg.Any<CancellationToken>());
        await _repository.Received(1).MarkAsFailedAsync(reminder.Id, Arg.Any<CancellationToken>());
    }
}