namespace SQLiteEncrypted.AvaloniaJournal.Data;

internal sealed record JournalEntry(
    long Id,
    string Title,
    string Body,
    string CreatedAt,
    string UpdatedAt)
{
    public string UpdatedDisplay =>
        DateTimeOffset.TryParse(UpdatedAt, out var value)
            ? value.ToLocalTime().ToString("g")
            : UpdatedAt;
}

