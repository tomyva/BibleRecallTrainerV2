using System.Collections.ObjectModel;
using System.Windows.Input;
using BibleRecallTrainerV2.Services;
using Microsoft.Maui.ApplicationModel;

namespace BibleRecallTrainerV2.ViewModels;

public sealed class DataToolsViewModel(
    IDataHealthService dataHealthService,
    IProgressExportService progressExportService,
    IExportShareService exportShareService,
    IReadingPositionStore readingPositionStore,
    IAppThemeService themeService) : ViewModelBase
{
    private string healthSummary = "Run a check to verify your saved progress.";
    private string exportStatus = string.Empty;
    private string bookmarkStatus = string.Empty;
    private string selectedTheme = themeService.CurrentTheme;
    private Command? checkCommand;
    private Command? exportCommand;

    public string HealthSummary { get => healthSummary; private set => SetProperty(ref healthSummary, value); }
    public string ExportStatus { get => exportStatus; private set => SetProperty(ref exportStatus, value); }
    public string BookmarkStatus { get => bookmarkStatus; private set => SetProperty(ref bookmarkStatus, value); }
    public IReadOnlyList<string> ThemeNames => themeService.ThemeNames;
    public string SelectedTheme
    {
        get => selectedTheme;
        set
        {
            if (SetProperty(ref selectedTheme, value))
                themeService.Apply(value);
        }
    }
    public string VersionText => $"Version {AppInfo.Current.VersionString}";
    public ObservableCollection<string> Issues { get; } = [];
    public ICommand CheckCommand => checkCommand ??= new Command(async () => await CheckAsync(), () => !IsBusy);
    public ICommand ExportCommand => exportCommand ??= new Command(async () => await ExportAsync(), () => !IsBusy);

    public Task LoadAsync() => CheckAsync();

    public void ResetReadingPositions()
    {
        readingPositionStore.ResetAll();
        BookmarkStatus = "All reading and listening positions were reset.";
    }

    private async Task CheckAsync()
    {
        SetBusy(true);
        ErrorMessage = string.Empty;
        try
        {
            var report = await dataHealthService.CheckAsync();
            HealthSummary = report.Summary;
            Issues.Clear();
            foreach (var issue in report.Issues)
                Issues.Add(issue);
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task ExportAsync()
    {
        SetBusy(true);
        ErrorMessage = string.Empty;
        ExportStatus = string.Empty;
        try
        {
            var json = await progressExportService.CreateJsonAsync();
            await exportShareService.ShareAsync(json);
            ExportStatus = "Progress export prepared successfully.";
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool value)
    {
        IsBusy = value;
        checkCommand?.ChangeCanExecute();
        exportCommand?.ChangeCanExecute();
    }
}
