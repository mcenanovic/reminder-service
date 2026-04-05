using ReminderService.Api.DTOs;
using ReminderService.Api.Models;

namespace ReminderService.Api.Services;

public interface IReminderService
{
    Task<Reminder> CreateReminderAsync(CreateReminderRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Reminder>> GetAllRemindersAsync(CancellationToken cancellationToken = default);
    Task ProcessDueRemindersAsync(CancellationToken cancellationToken = default);
}
