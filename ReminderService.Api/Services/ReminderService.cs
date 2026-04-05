using ReminderService.Api.DTOs;
using ReminderService.Api.Models;
using ReminderService.Api.Repositories;

namespace ReminderService.Api.Services;

public class ReminderService : IReminderService
{
    private const int MaxRetries = 3;

    private readonly IReminderRepository _repository;
    private readonly IReminderDeliveryService? _deliveryService;
    private readonly ILogger<ReminderService> _logger;

    public ReminderService(
        IReminderRepository repository,
        ILogger<ReminderService> logger,
        IReminderDeliveryService? deliveryService = null
    )
    {
        _repository = repository;
        _deliveryService = deliveryService;
        _logger = logger;
    }

    public async Task<Reminder> CreateReminderAsync(CreateReminderRequest request, CancellationToken cancellationToken = default)
    {
        if (request.SendAt <= DateTimeOffset.UtcNow)
        {
            throw new ArgumentException("SendAt must be a future UTC datetime.");
        }

        var reminder = new Reminder
        {
            Id = Guid.NewGuid(),
            Message = request.Message,
            SendAt = request.SendAt.ToUniversalTime(),
            Email = string.IsNullOrEmpty(request.Email) ? null : request.Email,
            Status = ReminderStatus.Scheduled
        };

        return await _repository.CreateAsync(reminder, cancellationToken);
    }

    public async Task<IReadOnlyList<Reminder>> GetAllRemindersAsync(CancellationToken cancellationToken = default)
    {
        return await _repository.GetAllAsync(cancellationToken);
    }

    public async Task ProcessDueRemindersAsync(CancellationToken cancellationToken = default)
    {
        var dueReminders = await _repository.GetDueRemindersAsync(cancellationToken);

        foreach (var reminder in dueReminders)
        {
            try
            {
                if (_deliveryService is not null)
                {
                    await _deliveryService.DeliverAsync(reminder);
                }

                var now = DateTimeOffset.UtcNow;
                _logger.LogInformation("[{Timestamp}] Reminder sent: {Message}", now.UtcDateTime.ToString(Converters.UtcDateTimeOffsetJsonConverter.OutputFormat), reminder.Message);
                await _repository.MarkAsSentAsync(reminder.Id, now, cancellationToken);
            }
            catch (Exception ex)
            {
                try
                {
                    await _repository.IncrementRetryCountAsync(reminder.Id, cancellationToken);

                    if (reminder.RetryCount + 1 >= MaxRetries)
                    {
                        _logger.LogError(ex, "Reminder {ReminderId} failed after {MaxRetries} attempts. Marking as Failed.", reminder.Id, MaxRetries);
                        await _repository.MarkAsFailedAsync(reminder.Id, cancellationToken);
                    }
                    else
                    {
                        _logger.LogWarning(ex, "Reminder {ReminderId} failed (attempt {Attempt}/{MaxRetries}). Will retry.", reminder.Id, reminder.RetryCount + 1, MaxRetries);
                    }
                }
                catch (Exception innerEx)
                {
                    _logger.LogError(innerEx, "Failed to update retry state for reminder {ReminderId}.", reminder.Id);
                }
            }
        }
    }
}