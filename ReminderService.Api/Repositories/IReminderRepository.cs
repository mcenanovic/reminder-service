using ReminderService.Api.Models;

namespace ReminderService.Api.Repositories;

public interface IReminderRepository
{
    Task<Reminder> CreateAsync(Reminder reminder);
    Task<IReadOnlyList<Reminder>> GetAllAsync();
    Task<IReadOnlyList<Reminder>> GetDueRemindersAsync();
    Task MarkAsSentAsync(Guid id, DateTimeOffset sentAt);
    Task IncrementRetryCountAsync(Guid id);
    Task MarkAsFailedAsync(Guid id);
}
