# Reminder Service

A .NET 8 Web API for scheduling and delivering simple reminders. Users can create reminders with a message, scheduled time, and optional email address. When the scheduled time is reached, reminders are automatically processed and marked as sent.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

No other dependencies are required. The application uses SQLite, which is embedded and requires no separate installation.

## Running the Application

```bash
dotnet run --project ReminderService.Api
```

The API will be available at `http://localhost:5232`. Swagger UI is available at `http://localhost:5232/swagger`.

To run with HTTPS enabled:

```bash
dotnet run --project ReminderService.Api --launch-profile https
```

This serves the API at `https://localhost:7148` (and `http://localhost:5232`). A trusted development certificate is required — run `dotnet dev-certs https --trust` if you haven't already.

## Running Tests

```bash
dotnet test
```

## Assumptions

- There is no authentication or authorization system. Anyone can create a reminder and view all reminders in the system.
- All application code is placed in a single API project, organized into folders by responsibility. A separate test project exists for unit and integration tests.
- Minimal APIs are used for endpoint definitions. Adding a library like FastEndpoints or using full MVC controllers would be unnecessary for two endpoints.
- Data annotations are used for request validation. Adding FluentValidation for this scope would be unnecessary overhead.
- Dapper is used over Entity Framework Core. For a single-table service, Dapper provides explicit SQL without the overhead of a full ORM.
- Reminder delivery is implemented as console logging by default. If a Brevo API key is configured, reminders with an email address will also trigger a real email via Brevo. It is assumed that a valid Brevo API key and an authorized sender email are available and can be configured in `appsettings.json`.
- Email is optional. Reminders without an email address are still valid and are delivered via console logging only.
- If Brevo is configured and email delivery fails, the reminder is treated as failed. This applies only when a Brevo API key is set; without it, reminders are delivered via console logging and cannot fail.
- A `BackgroundService` polls for due reminders every 30 seconds (configurable via `appsettings.json`). With a 30-second interval, reminders are delivered within one minute of their scheduled time. If the business allows more leniency, the polling interval can be increased.
- The background worker assumes the application is running continuously. If the application is stopped, any scheduled reminders that became due during downtime will be processed on the first poll after restart.
- The application is assumed to run as a single instance. There is no distributed locking or deduplication, so running multiple instances could result in duplicate reminder processing.
- If delivering a reminder fails, it is retried up to 3 times before being marked as "Failed." Retries happen on subsequent polling cycles with no additional delay, which is assumed to be acceptable. Failed reminders are not retried again.
- `SendAt` must be a future UTC datetime. Requests with past dates are rejected with a 400 response.
- `SendAt` accepts `DateTimeOffset` values in multiple ISO 8601 formats, including with or without timezone offsets and fractional seconds. When a timezone offset is provided (e.g., `+02:00`), it is respected and converted to UTC for storage. When no offset is provided, the system's local timezone is assumed. Date-only inputs (e.g., `"2026-04-12"`) are accepted and interpreted as midnight UTC.
- `Message` is required with a maximum length of 500 characters.
- `Email`, when provided, is validated using basic format validation, which is assumed to be sufficient for this scope. An empty string is treated the same as omitting the field entirely (no email).
- All datetime values are stored and handled in UTC, allowing users from different timezones to interact with the service consistently.
- Once created, a reminder cannot be cancelled, modified, or removed. Only creation and listing are supported as specified in the requirements.
- All reminders are returned in a single response with no pagination or filtering. For a production system with a growing dataset, pagination and filtering (e.g., by status or date range) would be necessary.
- There is no rate limiting. In a production environment, a rate limiting middleware would be appropriate.
- Sent and Failed reminders remain in the database indefinitely. A production system would benefit from an archival or cleanup policy.
- No log aggregation, monitoring, or alerting is in place. Logs are written to the console only.
