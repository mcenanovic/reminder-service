namespace ReminderService.Api.DTOs;

public record ReminderResponse(
    Guid Id,
    string Message,
    DateTimeOffset SendAt,
    string Status
);
