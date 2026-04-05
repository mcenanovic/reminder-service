using ReminderService.Api.Models;

namespace ReminderService.Api.Services;

public interface IReminderDeliveryService
{
    Task DeliverAsync(Reminder reminder);
}
