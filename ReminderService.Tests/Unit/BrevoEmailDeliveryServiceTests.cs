using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using ReminderService.Api.Configuration;
using ReminderService.Api.Models;
using ReminderService.Api.Services;

namespace ReminderService.Tests.Unit;

public class BrevoEmailDeliveryServiceTests
{
    private readonly BrevoEmailDeliveryService _service;
    private readonly ILogger<BrevoEmailDeliveryService> _logger;

    public BrevoEmailDeliveryServiceTests()
    {
        _logger = Substitute.For<ILogger<BrevoEmailDeliveryService>>();
        var settings = Options.Create(new BrevoSettings
        {
            ApiKey = "test-api-key",
            SenderName = "Test Service",
            SenderEmail = "test@sender.com"
        });
        _service = new BrevoEmailDeliveryService(settings, _logger);
    }

    [Fact]
    public async Task DeliverAsync_WithoutEmail_DoesNotCallApi()
    {
        var reminder = new Reminder
        {
            Id = Guid.NewGuid(),
            Message = "No email reminder",
            SendAt = DateTimeOffset.UtcNow
        };

        await _service.DeliverAsync(reminder);

        _logger.DidNotReceive().Log(
            Arg.Any<LogLevel>(),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task DeliverAsync_WithWhitespaceEmail_DoesNotCallApi()
    {
        var reminder = new Reminder
        {
            Id = Guid.NewGuid(),
            Message = "Whitespace email",
            Email = "   ",
            SendAt = DateTimeOffset.UtcNow
        };

        await _service.DeliverAsync(reminder);

        _logger.DidNotReceive().Log(
            Arg.Any<LogLevel>(),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task DeliverAsync_WithEmail_ThrowsOnInvalidApiKey()
    {
        var reminder = new Reminder
        {
            Id = Guid.NewGuid(),
            Message = "Test reminder",
            Email = "recipient@example.com",
            SendAt = DateTimeOffset.UtcNow
        };

        // With a fake API key, the Brevo SDK will throw an ApiException
        await Assert.ThrowsAnyAsync<Exception>(() => _service.DeliverAsync(reminder));
    }
}
