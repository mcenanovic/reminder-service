using ReminderService.Api.DTOs;
using ReminderService.Api.Models;

namespace ReminderService.Api.Services;

public interface IReminderService
{
    Task<Reminder> CreateReminderAsync(CreateReminderRequest request);
    Task<IReadOnlyList<Reminder>> GetAllRemindersAsync();
    Task ProcessDueRemindersAsync();
}
