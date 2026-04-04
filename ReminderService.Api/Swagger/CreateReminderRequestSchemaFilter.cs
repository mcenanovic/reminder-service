using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using ReminderService.Api.DTOs;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ReminderService.Api.Swagger;

public class CreateReminderRequestSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type != typeof(CreateReminderRequest)) return;

        var exampleTime = DateTimeOffset.UtcNow.AddHours(1).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");

        schema.Example = new OpenApiObject
        {
            ["message"] = new OpenApiString("Check API gateway logs"),
            ["sendAt"] = new OpenApiString(exampleTime),
            ["email"] = new OpenApiString("test@example.com")
        };
    }
}
