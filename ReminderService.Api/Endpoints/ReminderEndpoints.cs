using System.ComponentModel.DataAnnotations;
using ReminderService.Api.DTOs;
using ReminderService.Api.Services;

namespace ReminderService.Api.Endpoints;

public static class ReminderEndpoints
{
    public static void MapReminderEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/reminders").WithTags("Reminders");

        group.MapPost("/", async (CreateReminderRequest request, IReminderService service, CancellationToken cancellationToken) =>
        {
            var validationResults = new List<ValidationResult>();
            if (!Validator.TryValidateObject(request, new ValidationContext(request), validationResults, true))
            {
                var errors = validationResults.Select(v => v.ErrorMessage);
                return Results.BadRequest(new { errors });
            }

            var reminder = await service.CreateReminderAsync(request, cancellationToken);

            return Results.Created((string?)null, new CreateReminderResponse(
                reminder.Id,
                reminder.Status.ToString(),
                reminder.SendAt
                )
            );
        })
        .WithName("CreateReminder")
        .WithDescription("Creates a new reminder with a message, scheduled time, and optional email address.")
        .Produces<CreateReminderResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .WithOpenApi();

        group.MapGet("/", async (IReminderService service, CancellationToken cancellationToken) =>
        {
            var reminders = await service.GetAllRemindersAsync(cancellationToken);

            return Results.Ok(reminders.Select(r => new ReminderResponse(
                r.Id,
                r.Message,
                r.SendAt,
                r.Status.ToString()))
            );
        })
        .WithName("GetReminders")
        .WithDescription("Returns all reminders in the system, ordered by scheduled time descending.")
        .Produces<IEnumerable<ReminderResponse>>(StatusCodes.Status200OK)
        .WithOpenApi();
    }
}
