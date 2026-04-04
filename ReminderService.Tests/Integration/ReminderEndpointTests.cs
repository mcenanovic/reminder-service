using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using ReminderService.Api.DTOs;
using ReminderService.Api.Services;

namespace ReminderService.Tests.Integration;

public class ReminderEndpointTests : IClassFixture<TestApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestApplicationFactory _factory;

    public ReminderEndpointTests(TestApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateReminder_WithValidRequest_Returns201()
    {
        var request = new CreateReminderRequest(
            Message: "Integration test reminder",
            SendAt: DateTimeOffset.UtcNow.AddHours(1),
            Email: "test@example.com"
        );

        var response = await _client.PostAsJsonAsync("/reminders", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<CreateReminderResponse>();
        Assert.NotNull(result);
        Assert.Equal("Scheduled", result.Status);
        Assert.NotEqual(Guid.Empty, result.Id);
    }

    [Fact]
    public async Task CreateReminder_WithPastDate_Returns400()
    {
        var request = new CreateReminderRequest(
            Message: "Past reminder",
            SendAt: DateTimeOffset.UtcNow.AddHours(-1)
        );

        var response = await _client.PostAsJsonAsync("/reminders", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetReminders_ReturnsAllReminders()
    {
        var request = new CreateReminderRequest(
            Message: "List test reminder",
            SendAt: DateTimeOffset.UtcNow.AddHours(2)
        );

        await _client.PostAsJsonAsync("/reminders", request);

        var response = await _client.GetAsync("/reminders");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var reminders = await response.Content.ReadFromJsonAsync<List<ReminderResponse>>();
        Assert.NotNull(reminders);
        Assert.Contains(reminders, r => r.Message == "List test reminder");
    }

    [Fact]
    public async Task GetReminders_WhenEmpty_ReturnsEmptyArray()
    {
        // Use a fresh factory with its own DB to guarantee no reminders exist
        await using var freshFactory = new TestApplicationFactory();
        var client = freshFactory.CreateClient();

        var response = await client.GetAsync("/reminders");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var reminders = await response.Content.ReadFromJsonAsync<List<ReminderResponse>>();
        Assert.NotNull(reminders);
        Assert.Empty(reminders);
    }

    [Fact]
    public async Task CreateReminder_WithEmptyEmail_Returns201()
    {
        var response = await _client.PostAsJsonAsync("/reminders", new
        {
            message = "Empty email test",
            sendAt = DateTimeOffset.UtcNow.AddHours(1),
            email = ""
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateReminder_WithEmptyMessage_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/reminders", new
        {
            message = "",
            sendAt = DateTimeOffset.UtcNow.AddHours(1)
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateReminder_WithInvalidEmail_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/reminders", new
        {
            message = "Test",
            sendAt = DateTimeOffset.UtcNow.AddHours(1),
            email = "not-an-email"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateReminder_WithEmailMissingTld_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/reminders", new
        {
            message = "Test",
            sendAt = DateTimeOffset.UtcNow.AddHours(1),
            email = "user@example"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateReminder_WithMaxLengthMessage_Returns201()
    {
        var maxMessage = new string('A', 500);

        var response = await _client.PostAsJsonAsync("/reminders", new
        {
            message = maxMessage,
            sendAt = DateTimeOffset.UtcNow.AddHours(1)
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<CreateReminderResponse>();
        Assert.NotNull(result);
        Assert.Equal("Scheduled", result.Status);
    }

    [Fact]
    public async Task CreateReminder_WithMessageExceedingMaxLength_Returns400()
    {
        var tooLongMessage = new string('A', 501);

        var response = await _client.PostAsJsonAsync("/reminders", new
        {
            message = tooLongMessage,
            sendAt = DateTimeOffset.UtcNow.AddHours(1)
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateReminder_WithTimezoneOffset_Returns201AndConvertsToUtc()
    {
        // Send a time with +05:00 offset - should be accepted and stored as UTC
        var futureUtc = DateTimeOffset.UtcNow.AddHours(2);
        var withOffset = futureUtc.ToOffset(TimeSpan.FromHours(5));

        var response = await _client.PostAsJsonAsync("/reminders", new
        {
            message = "Offset test",
            sendAt = withOffset
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<CreateReminderResponse>();
        Assert.NotNull(result);
        Assert.Equal("Scheduled", result.Status);
        // The stored time should represent the same instant
        Assert.True(Math.Abs((result.SendAt - futureUtc).TotalSeconds) < 2, "Stored time should match the original instant regardless of offset");
    }

    [Theory]
    [InlineData("2027-04-04T18:27:12.407Z")]
    [InlineData("2027-04-04T18:27:12.407+02:00")]
    [InlineData("2027-04-04T18:27:12.407")]
    [InlineData("2027-04-04")]
    public async Task CreateReminder_WithValidDateFormat_Returns201(string sendAt)
    {
        var json = $$"""{"message": "Date format test", "sendAt": "{{sendAt}}"}""";
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/reminders", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory]
    [InlineData("04/04/2027 12:12")]
    [InlineData("not-a-date")]
    public async Task CreateReminder_WithInvalidDateFormat_Returns400(string sendAt)
    {
        var json = $$"""{"message": "Date format test", "sendAt": "{{sendAt}}"}""";
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/reminders", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task FullFlow_CreateAndProcess_StatusBecomesSent()
    {
        // Insert directly via repository to bypass future-date validation
        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<Api.Repositories.IReminderRepository>();
        var service = scope.ServiceProvider.GetRequiredService<IReminderService>();

        var reminder = new Api.Models.Reminder
        {
            Id = Guid.NewGuid(),
            Message = "Flow test reminder",
            SendAt = DateTimeOffset.UtcNow.AddSeconds(-10),
            Status = Api.Models.ReminderStatus.Scheduled
        };
        await repository.CreateAsync(reminder);

        await service.ProcessDueRemindersAsync();

        var response = await _client.GetAsync("/reminders");
        var reminders = await response.Content.ReadFromJsonAsync<List<ReminderResponse>>();

        Assert.NotNull(reminders);
        var processed = reminders.FirstOrDefault(r => r.Id == reminder.Id);
        Assert.NotNull(processed);
        Assert.Equal("Sent", processed.Status);
    }
}
