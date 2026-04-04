namespace ReminderService.Api.Models;

public class Reminder
{
    public Guid Id { get; init; }
    public string Message { get; init; } = string.Empty;
    public DateTimeOffset SendAt { get; init; }
    public string? Email { get; init; }
    public ReminderStatus Status { get; init; } = ReminderStatus.Scheduled;
    public int RetryCount { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? SentAt { get; init; }
}
