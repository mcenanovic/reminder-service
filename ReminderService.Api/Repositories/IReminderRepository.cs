using ReminderService.Api.Models;

namespace ReminderService.Api.Repositories;

public interface IReminderRepository
{
    Task<Reminder> CreateAsync(Reminder reminder, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Reminder>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Reminder>> GetDueRemindersAsync(CancellationToken cancellationToken = default);
    Task MarkAsSentAsync(Guid id, DateTimeOffset sentAt, CancellationToken cancellationToken = default);
    Task IncrementRetryCountAsync(Guid id, CancellationToken cancellationToken = default);
    Task MarkAsFailedAsync(Guid id, CancellationToken cancellationToken = default);
}
