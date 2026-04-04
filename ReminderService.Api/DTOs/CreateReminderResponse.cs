namespace ReminderService.Api.DTOs;

public record CreateReminderResponse(
    Guid Id,
    string Status,
    DateTimeOffset SendAt
);
