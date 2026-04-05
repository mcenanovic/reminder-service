namespace ReminderService.Api.Configuration;

public class BrevoSettings
{
    public const string SectionName = "Brevo";

    public string? ApiKey { get; init; }
    public string? SenderName { get; init; }
    public string? SenderEmail { get; init; }
}
