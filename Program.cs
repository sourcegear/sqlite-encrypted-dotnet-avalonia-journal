using Avalonia;
using SQLitePCL;

namespace SQLiteEncrypted.AvaloniaJournal;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        raw.SetProvider(new SQLite3Provider_e_sqlite3());
        raw.FreezeProvider();
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}

