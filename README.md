# SQLite.Encrypted Avalonia desktop journal

This sample is a cross-platform desktop notes application built with Avalonia
and the published `SQLite.Encrypted` NuGet package. It demonstrates encrypted
CRUD and search on Windows, macOS, and Linux.

Database code is isolated in `Data/`. It uses `Microsoft.Data.Sqlite.Core` for
idiomatic ADO.NET connections, commands, parameters, and readers. The
`SQLitePCLRaw.provider.e_sqlite3` package selects the native `e_sqlite3`
library supplied by `SQLite.Encrypted`. Avalonia code stays in
`MainWindow.axaml` and its code-behind.

## Requirements

- .NET 8 SDK or later
- a SQLite.Encrypted activation key valid for the current platform and SQLite 3.54

Free fully-featured demo keys are available from
[sqlite.sourcegear.com](https://sqlite.sourcegear.com/#evaluation).

## Run

The activation key can be entered in the unlock screen. To prefill it without
putting it in source code, set this environment variable:

```powershell
$env:SQLITE_ENCRYPTED_ACTIVATION_KEY = '<activation key without the see- prefix>'
```

Then restore and run:

```powershell
cd sqlite-encrypted-dotnet-avalonia-journal
dotnet restore
dotnet run
```

Enter a database passphrase in the unlock screen. The encrypted database is
stored in the current user's local application-data folder under
`SQLiteEncrypted/AvaloniaJournal/journal.db`. The sample does not persist the
activation key or database passphrase.

## Security behavior

Opening the journal performs this checked sequence:

1. Configure `SQLite3Provider_e_sqlite3` before starting Avalonia.
2. Open a non-pooled `Microsoft.Data.Sqlite.SqliteConnection`.
3. Activate SEE with `PRAGMA activate_extensions = 'see-…'`.
4. Run `PRAGMA key` and require its first result to be `ok`.
5. Read `sqlite_master` so a wrong passphrase fails before schema creation.

An invalid activation attempt affects the process-global SEE state. The sample
therefore disables another unlock attempt and tells the user to restart the
application.

## Publish

Publish for the intended desktop runtime so .NET selects the matching native
`e_sqlite3` asset:

```powershell
dotnet publish -c Release -r win-x64 --self-contained false
dotnet publish -c Release -r linux-x64 --self-contained false
dotnet publish -c Release -r osx-arm64 --self-contained false
```

