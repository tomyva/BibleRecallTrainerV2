using System.Collections.ObjectModel;
using System.Windows.Input;
using BibleRecallTrainerV2.Models;
using BibleRecallTrainerV2.Services;

namespace BibleRecallTrainerV2.ViewModels;

public sealed class ReminderTimeViewModel(TimeSpan time) : ViewModelBase
{
    private TimeSpan timeOfDay = time;
    public TimeSpan TimeOfDay { get => timeOfDay; set => SetProperty(ref timeOfDay, value); }
}

public sealed class ReminderViewModel : ViewModelBase
{
    private const int MaximumReminderCount = 8;
    private readonly IReminderService reminderService;
    private bool isEnabled;
    private string statusMessage = string.Empty;
    private Command? saveCommand;
    private Command? addTimeCommand;
    private Command<ReminderTimeViewModel>? removeTimeCommand;

    public ReminderViewModel(IReminderService reminderService)
    {
        this.reminderService = reminderService;
    }

    public bool IsSupported => reminderService.IsSupported;
    public bool IsEnabled { get => isEnabled; set => SetProperty(ref isEnabled, value); }
    public string StatusMessage { get => statusMessage; private set => SetProperty(ref statusMessage, value); }
    public ObservableCollection<ReminderTimeViewModel> ReminderTimes { get; } = [];
    public ICommand SaveCommand => saveCommand ??= new Command(async () => await SaveAsync(), () => !IsBusy);
    public ICommand AddTimeCommand => addTimeCommand ??= new Command(AddTime, () => ReminderTimes.Count < MaximumReminderCount && !IsBusy);
    public ICommand RemoveTimeCommand => removeTimeCommand ??= new Command<ReminderTimeViewModel>(RemoveTime, _ => ReminderTimes.Count > 1 && !IsBusy);

    public void Load()
    {
        var settings = reminderService.GetSettings();
        IsEnabled = settings.IsEnabled;
        ReminderTimes.Clear();
        foreach (var time in settings.TimesOfDay.Order())
            ReminderTimes.Add(new ReminderTimeViewModel(time));
        if (ReminderTimes.Count == 0)
            ReminderTimes.Add(new ReminderTimeViewModel(ReminderSettings.Default.TimesOfDay[0]));

        StatusMessage = reminderService.IsSupported
            ? "Add the times that fit your day, then save."
            : "Daily reminders are not supported on this device.";
        RefreshCommands();
    }

    private void AddTime()
    {
        var time = ReminderTimes.Count == 0
            ? ReminderSettings.Default.TimesOfDay[0]
            : TimeSpan.FromMinutes((ReminderTimes[^1].TimeOfDay.TotalMinutes + 240) % (24 * 60));
        ReminderTimes.Add(new ReminderTimeViewModel(time));
        RefreshCommands();
    }

    private void RemoveTime(ReminderTimeViewModel? reminder)
    {
        if (reminder is null || ReminderTimes.Count <= 1)
            return;
        ReminderTimes.Remove(reminder);
        RefreshCommands();
    }

    private async Task SaveAsync()
    {
        IsBusy = true;
        RefreshCommands();
        ErrorMessage = string.Empty;
        try
        {
            var times = ReminderTimes.Select(item => item.TimeOfDay).ToList();
            StatusMessage = await reminderService.SaveAsync(new ReminderSettings(IsEnabled, times));
            var saved = reminderService.GetSettings();
            IsEnabled = saved.IsEnabled;
            ReminderTimes.Clear();
            foreach (var time in saved.TimesOfDay.Order())
                ReminderTimes.Add(new ReminderTimeViewModel(time));
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
        }
        finally
        {
            IsBusy = false;
            RefreshCommands();
        }
    }

    private void RefreshCommands()
    {
        saveCommand?.ChangeCanExecute();
        addTimeCommand?.ChangeCanExecute();
        removeTimeCommand?.ChangeCanExecute();
    }
}
