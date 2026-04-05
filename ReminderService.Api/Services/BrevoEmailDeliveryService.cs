using brevo_csharp.Model;
using Microsoft.Extensions.Options;
using ReminderService.Api.Configuration;
using ReminderService.Api.Models;
using Task = System.Threading.Tasks.Task;

namespace ReminderService.Api.Services;

public class BrevoEmailDeliveryService : IReminderDeliveryService
{
    private readonly BrevoSettings _settings;
    private readonly ILogger<BrevoEmailDeliveryService> _logger;

    public BrevoEmailDeliveryService(IOptions<BrevoSettings> settings, ILogger<BrevoEmailDeliveryService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task DeliverAsync(Reminder reminder)
    {
        if (string.IsNullOrWhiteSpace(reminder.Email)) return;

        var configuration = new brevo_csharp.Client.Configuration();
        configuration.AddApiKey("api-key", _settings.ApiKey);

        var apiInstance = new brevo_csharp.Api.TransactionalEmailsApi(configuration);

        var email = new SendSmtpEmail(
            sender: new SendSmtpEmailSender(_settings.SenderName, _settings.SenderEmail),
            to: [new SendSmtpEmailTo(reminder.Email)],
            subject: "A New Reminder",
            htmlContent: BuildHtmlBody(reminder)
        );

        await apiInstance.SendTransacEmailAsync(email);
        _logger.LogInformation("Email sent to {Email} with a reminder: {Reminder}", reminder.Email, reminder.Message);
    }

    private static string BuildHtmlBody(Reminder reminder)
    {
        var formattedTime = reminder.SendAt.UtcDateTime.ToString("yyyy-MM-dd HH:mm 'UTC'");

        return $"""
            <html>
            <body style="font-family: Arial, sans-serif; color: #333;">
                <p>{System.Net.WebUtility.HtmlEncode(reminder.Message)}</p>
                <p style="font-size: 12px; color: #888;">Scheduled for: {formattedTime}</p>
            </body>
            </html>
            """;
    }
}
