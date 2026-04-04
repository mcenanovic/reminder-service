using Microsoft.Data.Sqlite;

namespace ReminderService.Api.Database;

public static class DatabaseInitializer
{
    public static void Initialize(string connectionString)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Reminders (
                Id TEXT PRIMARY KEY,
                Message TEXT NOT NULL,
                SendAt TEXT NOT NULL,
                Email TEXT,
                Status TEXT NOT NULL DEFAULT 'Scheduled',
                RetryCount INTEGER NOT NULL DEFAULT 0,
                CreatedAt TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
                SentAt TEXT
            );

            CREATE INDEX IF NOT EXISTS IX_Reminders_Status_SendAt
                ON Reminders (Status, SendAt);
            """;
        command.ExecuteNonQuery();
    }
}
