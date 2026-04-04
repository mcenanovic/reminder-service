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
