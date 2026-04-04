using System.ComponentModel.DataAnnotations;
using ReminderService.Api.Attributes;

namespace ReminderService.Api.DTOs;

public record CreateReminderRequest(
    [property: Required, MaxLength(500)] string Message,
    [property: Required] DateTimeOffset SendAt,
    [property: StrictEmailAddress] string? Email = null
);
