using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ReminderService.Api.Services;
using ReminderService.Api.Workers;

namespace ReminderService.Tests.Integration;

public class ExceptionHandlingTests : IClassFixture<ExceptionHandlingTests.FaultyServiceFactory>
{
    private readonly HttpClient _client;

    public ExceptionHandlingTests(FaultyServiceFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetReminders_WhenServiceThrows_Returns500WithGenericMessage()
    {
        var response = await _client.GetAsync("/reminders");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(body);
        Assert.Equal("An unexpected error occurred.", body.Error);
    }

    [Fact]
    public async Task CreateReminder_WhenServiceThrows_Returns500WithGenericMessage()
    {
        var response = await _client.PostAsJsonAsync("/reminders", new
        {
            message = "Test",
            sendAt = DateTime.UtcNow.AddHours(1)
        });

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(body);
        Assert.Equal("An unexpected error occurred.", body.Error);
    }

    private record ErrorResponse(string Error);

    public class FaultyServiceFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"reminder_fault_{Guid.NewGuid()}.db");

        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove worker
                var workerDescriptor = services.SingleOrDefault(d => d.ImplementationType == typeof(ReminderWorker));
                if (workerDescriptor is not null)
                {
                    services.Remove(workerDescriptor);
                }

                // Replace service with one that always throws
                var serviceDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IReminderService));
                if (serviceDescriptor is not null)
                {
                    services.Remove(serviceDescriptor);
                }

                var faultyService = Substitute.For<IReminderService>();
                faultyService.GetAllRemindersAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new Exception("Database connection failed"));
                faultyService.CreateReminderAsync(null!, Arg.Any<CancellationToken>()).ThrowsAsyncForAnyArgs(new Exception("Database connection failed"));

                services.AddScoped(_ => faultyService);
            });

            return base.CreateHost(builder);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={_dbPath}");
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (File.Exists(_dbPath))
            {
                File.Delete(_dbPath);
            }
        }
    }
}
