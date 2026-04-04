using Dapper;
using Microsoft.Data.Sqlite;
using ReminderService.Api.Models;

namespace ReminderService.Api.Repositories;

public class ReminderRepository : IReminderRepository
{
    private readonly string _connectionString;

    public ReminderRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    public async Task<Reminder> CreateAsync(Reminder reminder, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);

        const string sql = """
            INSERT INTO Reminders (Id, Message, SendAt, Email, Status)
            VALUES (@Id, @Message, @SendAt, @Email, @Status);
            """;

        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            Id = reminder.Id.ToString(),
            reminder.Message,
            SendAt = reminder.SendAt.UtcDateTime.ToString("o"),
            reminder.Email,
            Status = reminder.Status.ToString()
        }, cancellationToken: cancellationToken));

        return reminder;
    }

    public async Task<IReadOnlyList<Reminder>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);

        const string sql = "SELECT * FROM Reminders ORDER BY SendAt DESC;";

        var rows = await connection.QueryAsync<ReminderRow>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return rows.Select(MapToReminder).ToList();
    }

    public async Task<IReadOnlyList<Reminder>> GetDueRemindersAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);

        const string sql = """
            SELECT * FROM Reminders
            WHERE Status = 'Scheduled' AND SendAt <= @Now;
            """;

        var rows = await connection.QueryAsync<ReminderRow>(new CommandDefinition(sql, new { Now = DateTimeOffset.UtcNow.UtcDateTime.ToString("o") }, cancellationToken: cancellationToken));
        return rows.Select(MapToReminder).ToList();
    }

    public async Task MarkAsSentAsync(Guid id, DateTimeOffset sentAt, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);

        const string sql = """
            UPDATE Reminders
            SET Status = 'Sent', SentAt = @SentAt
            WHERE Id = @Id;
            """;

        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            Id = id.ToString(),
            SentAt = sentAt.UtcDateTime.ToString("o")
        }, cancellationToken: cancellationToken));
    }

    public async Task IncrementRetryCountAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);

        const string sql = """
            UPDATE Reminders
            SET RetryCount = RetryCount + 1
            WHERE Id = @Id;
            """;

        await connection.ExecuteAsync(new CommandDefinition(sql, new { Id = id.ToString() }, cancellationToken: cancellationToken));
    }

    public async Task MarkAsFailedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);

        const string sql = """
            UPDATE Reminders
            SET Status = 'Failed'
            WHERE Id = @Id;
            """;

        await connection.ExecuteAsync(new CommandDefinition(sql, new { Id = id.ToString() }, cancellationToken: cancellationToken));
    }

    private static Reminder MapToReminder(ReminderRow row) => new()
    {
        Id = Guid.Parse(row.Id),
        Message = row.Message,
        SendAt = DateTimeOffset.Parse(row.SendAt),
        Email = row.Email,
        Status = Enum.Parse<ReminderStatus>(row.Status),
        RetryCount = (int)row.RetryCount,
        CreatedAt = DateTimeOffset.Parse(row.CreatedAt),
        SentAt = row.SentAt is not null ? DateTimeOffset.Parse(row.SentAt) : null
    };

    private record ReminderRow(
        string Id,
        string Message,
        string SendAt,
        string? Email,
        string Status,
        long RetryCount,
        string CreatedAt,
        string? SentAt);
}
