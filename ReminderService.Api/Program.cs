using ReminderService.Api.Configuration;
using ReminderService.Api.Converters;
using ReminderService.Api.Swagger;
using ReminderService.Api.Database;
using ReminderService.Api.Endpoints;
using ReminderService.Api.Middleware;
using ReminderService.Api.Repositories;
using ReminderService.Api.Services;
using ReminderService.Api.Workers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SchemaFilter<CreateReminderRequestSchemaFilter>();
});
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new UtcDateTimeOffsetJsonConverter());
});

builder.Services.AddScoped<IReminderRepository, ReminderRepository>();
builder.Services.AddScoped<IReminderService, ReminderService.Api.Services.ReminderService>();
builder.Services.AddHostedService<ReminderWorker>();

builder.Services.Configure<BrevoSettings>(builder.Configuration.GetSection(BrevoSettings.SectionName));

var brevoSettings = builder.Configuration.GetSection(BrevoSettings.SectionName).Get<BrevoSettings>();
if (!string.IsNullOrWhiteSpace(brevoSettings?.ApiKey))
{
    if (string.IsNullOrWhiteSpace(brevoSettings.SenderName) || string.IsNullOrWhiteSpace(brevoSettings.SenderEmail))
    {
        throw new InvalidOperationException("Brevo API key is configured but SenderName and/or SenderEmail are missing. All three values are required. To disable Brevo, leave the ApiKey empty.");
    }

    builder.Services.AddScoped<IReminderDeliveryService, BrevoEmailDeliveryService>();
}

var app = builder.Build();

var connectionString = app.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
DatabaseInitializer.Initialize(connectionString);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.MapReminderEndpoints();

app.Run();

// Required for WebApplicationFactory<Program> in integration tests
public partial class Program;
