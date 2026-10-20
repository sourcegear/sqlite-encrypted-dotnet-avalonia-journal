using Avalonia.Controls;
using Avalonia.Interactivity;
using SQLiteEncrypted.AvaloniaJournal.Data;

namespace SQLiteEncrypted.AvaloniaJournal;

public sealed partial class MainWindow : Window
{
    private JournalRepository? _repository;
    private long? _selectedId;

    public MainWindow()
    {
        InitializeComponent();
        ActivationKeyBox.Text = Environment.GetEnvironmentVariable(
            "SQLITE_ENCRYPTED_ACTIVATION_KEY") ?? string.Empty;
        Closed += (_, _) => _repository?.Dispose();
    }

    private string DatabasePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SQLiteEncrypted",
        "AvaloniaJournal",
        "journal.db");

    private void UnlockClicked(object? sender, RoutedEventArgs e)
    {
        UnlockErrorBorder.IsVisible = false;
        var activationKey = ActivationKeyBox.Text?.Trim() ?? string.Empty;
        var databaseKey = DatabaseKeyBox.Text ?? string.Empty;
        if (activationKey.Length == 0 || databaseKey.Length == 0)
        {
            ShowUnlockError("Both the activation key and database passphrase are required.");
            return;
        }

        try
        {
            _repository = new JournalRepository(DatabasePath, activationKey, databaseKey);
            ActivationKeyBox.Text = string.Empty;
            DatabaseKeyBox.Text = string.Empty;
            UnlockCard.IsVisible = false;
            JournalWorkspace.IsVisible = true;
            ReloadNotes();
            NewNote();
        }
        catch (SeeActivationException failure)
        {
            UnlockButton.IsEnabled = false;
            ShowUnlockError(failure.Message);
        }
        catch (Exception failure)
        {
            ShowUnlockError(
                "The journal could not be opened. If it already exists, verify the " +
                $"database passphrase. Details: {failure.Message}");
        }
    }

    private void NewClicked(object? sender, RoutedEventArgs e) => NewNote();

    private void SaveClicked(object? sender, RoutedEventArgs e)
    {
        if (_repository is null)
        {
            return;
        }

        var title = TitleBox.Text?.Trim() ?? string.Empty;
        var body = BodyBox.Text?.Trim() ?? string.Empty;
        if (title.Length == 0)
        {
            StatusText.Text = "Enter a title before saving.";
            return;
        }

        try
        {
            if (_selectedId is long id)
            {
                _repository.Update(id, title, body);
                StatusText.Text = "Note updated.";
            }
            else
            {
                var note = _repository.Add(title, body);
                _selectedId = note.Id;
                StatusText.Text = "Note added.";
            }

            ReloadNotes(_selectedId);
        }
        catch (Exception failure)
        {
            StatusText.Text = $"Save failed: {failure.Message}";
        }
    }

    private void DeleteClicked(object? sender, RoutedEventArgs e)
    {
        if (_repository is null || _selectedId is not long id)
        {
            return;
        }

        try
        {
            _repository.Delete(id);
            ReloadNotes();
            NewNote();
            StatusText.Text = "Note deleted.";
        }
        catch (Exception failure)
        {
            StatusText.Text = $"Delete failed: {failure.Message}";
        }
    }

    private void SearchChanged(object? sender, TextChangedEventArgs e) => ReloadNotes();

    private void NoteSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (NotesList.SelectedItem is not JournalEntry entry)
        {
            return;
        }

        _selectedId = entry.Id;
        EditorHeading.Text = "Edit note";
        TitleBox.Text = entry.Title;
        BodyBox.Text = entry.Body;
        DeleteButton.IsVisible = true;
        StatusText.Text = $"Last updated {entry.UpdatedDisplay}";
    }

    private void ReloadNotes(long? selectId = null)
    {
        if (_repository is null)
        {
            return;
        }

        var notes = _repository.List(SearchBox.Text);
        NotesList.ItemsSource = notes;
        NoteCountText.Text = notes.Count == 1 ? "1 note" : $"{notes.Count} notes";

        if (selectId is long id)
        {
            NotesList.SelectedItem = notes.FirstOrDefault(note => note.Id == id);
        }
    }

    private void NewNote()
    {
        _selectedId = null;
        NotesList.SelectedItem = null;
        EditorHeading.Text = "New note";
        TitleBox.Text = string.Empty;
        BodyBox.Text = string.Empty;
        DeleteButton.IsVisible = false;
        StatusText.Text = "Ready";
        TitleBox.Focus();
    }

    private void ShowUnlockError(string message)
    {
        UnlockErrorText.Text = message;
        UnlockErrorBorder.IsVisible = true;
    }
}

