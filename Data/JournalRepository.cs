using Microsoft.Data.Sqlite;

namespace SQLiteEncrypted.AvaloniaJournal.Data;

internal sealed class JournalRepository : IDisposable
{
    private readonly SqliteConnection _connection;

    internal JournalRepository(
        string path,
        string activationKey,
        string databaseKey)
    {
        _connection = EncryptedConnection.Open(path, activationKey, databaseKey);
        try
        {
            using var command = CreateCommand("""
                CREATE TABLE IF NOT EXISTS journal_entries (
                    id INTEGER PRIMARY KEY,
                    title TEXT NOT NULL,
                    body TEXT NOT NULL,
                    created_at TEXT NOT NULL,
                    updated_at TEXT NOT NULL
                )
                """);
            command.ExecuteNonQuery();
        }
        catch
        {
            _connection.Dispose();
            throw;
        }
    }

    internal JournalEntry Add(string title, string body)
    {
        const string sql = """
            INSERT INTO journal_entries(title, body, created_at, updated_at)
            VALUES ($title, $body, $createdAt, $updatedAt);
            SELECT last_insert_rowid();
            """;
        var now = DateTimeOffset.UtcNow.ToString("O");
        using var command = CreateCommand(sql);
        command.Parameters.AddWithValue("$title", title);
        command.Parameters.AddWithValue("$body", body);
        command.Parameters.AddWithValue("$createdAt", now);
        command.Parameters.AddWithValue("$updatedAt", now);
        var id = (long)(command.ExecuteScalar()
            ?? throw new InvalidOperationException("SQLite did not return the inserted ID."));
        return Get(id)
            ?? throw new InvalidOperationException("The inserted note was not found.");
    }

    internal IReadOnlyList<JournalEntry> List(string? search = null)
    {
        var sql = string.IsNullOrWhiteSpace(search)
            ? """
                SELECT id, title, body, created_at, updated_at
                FROM journal_entries ORDER BY updated_at DESC, id DESC
                """
            : """
                SELECT id, title, body, created_at, updated_at
                FROM journal_entries
                WHERE title LIKE $search ESCAPE '\' OR body LIKE $search ESCAPE '\'
                ORDER BY updated_at DESC, id DESC
                """;

        using var command = CreateCommand(sql);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var escaped = search.Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("%", "\\%", StringComparison.Ordinal)
                .Replace("_", "\\_", StringComparison.Ordinal);
            command.Parameters.AddWithValue("$search", $"%{escaped}%");
        }

        var entries = new List<JournalEntry>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            entries.Add(ReadEntry(reader));
        }
        return entries;
    }

    internal JournalEntry? Get(long id)
    {
        using var command = CreateCommand("""
            SELECT id, title, body, created_at, updated_at
            FROM journal_entries WHERE id = $id
            """);
        command.Parameters.AddWithValue("$id", id);
        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadEntry(reader) : null;
    }

    internal bool Update(long id, string title, string body)
    {
        using var command = CreateCommand("""
            UPDATE journal_entries
            SET title = $title, body = $body, updated_at = $updatedAt
            WHERE id = $id
            """);
        command.Parameters.AddWithValue("$title", title);
        command.Parameters.AddWithValue("$body", body);
        command.Parameters.AddWithValue("$updatedAt", DateTimeOffset.UtcNow.ToString("O"));
        command.Parameters.AddWithValue("$id", id);
        return command.ExecuteNonQuery() == 1;
    }

    internal bool Delete(long id)
    {
        using var command = CreateCommand(
            "DELETE FROM journal_entries WHERE id = $id");
        command.Parameters.AddWithValue("$id", id);
        return command.ExecuteNonQuery() == 1;
    }

    private SqliteCommand CreateCommand(string sql)
    {
        var command = _connection.CreateCommand();
        command.CommandText = sql;
        return command;
    }

    private static JournalEntry ReadEntry(SqliteDataReader reader) => new(
        reader.GetInt64(0),
        reader.GetString(1),
        reader.GetString(2),
        reader.GetString(3),
        reader.GetString(4));

    public void Dispose() => _connection.Dispose();
}
