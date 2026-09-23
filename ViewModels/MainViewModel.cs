using System.Windows.Input;
using BibleRecallTrainerV2.Models;
using BibleRecallTrainerV2.Services;

namespace BibleRecallTrainerV2.ViewModels;

public sealed class MainViewModel(IStudyPlanService studyPlanService) : ViewModelBase
{
    private StudyPlanSnapshot snapshot = new();

    public StudyPlanSnapshot Snapshot
    {
        get => snapshot;
        private set
        {
            if (SetProperty(ref snapshot, value))
                RefreshCommands();
        }
    }

    public ICommand BrowseBooksCommand { get; } = new Command(async () => await Shell.Current.GoToAsync(nameof(BooksPage)));
    public ICommand OpenStudyPlanCommand { get; } = new Command(async () => await Shell.Current.GoToAsync(nameof(StudyPlanPage)));
    public ICommand OpenReminderCommand { get; } = new Command(async () => await Shell.Current.GoToAsync(nameof(ReminderPage)));
    public ICommand OpenDataToolsCommand { get; } = new Command(async () => await Shell.Current.GoToAsync(nameof(DataToolsPage)));
    public ICommand ContinueCommand => continueCommand ??= new Command(async () => await OpenChapterAsync(Snapshot.NextChapter), () => Snapshot.NextChapter is not null);
    public ICommand ReviewDueCommand => reviewDueCommand ??= new Command(async () => await OpenQuizAsync(Snapshot.DueReviews.FirstOrDefault()), () => Snapshot.DueReviews.Count > 0);

    private Command? continueCommand;
    private Command? reviewDueCommand;

    public async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            Snapshot = await studyPlanService.GetSnapshotAsync();
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static Task OpenChapterAsync(StudyChapterItem? chapter) => chapter is null
        ? Task.CompletedTask
        : Shell.Current.GoToAsync($"{nameof(ChapterReaderPage)}?bookId={Uri.EscapeDataString(chapter.BookId)}&chapter={chapter.ChapterNumber}");

    private static Task OpenQuizAsync(StudyChapterItem? chapter) => chapter is null
        ? Task.CompletedTask
        : Shell.Current.GoToAsync($"{nameof(QuizPage)}?bookId={Uri.EscapeDataString(chapter.BookId)}&chapter={chapter.ChapterNumber}");

    private void RefreshCommands()
    {
        continueCommand?.ChangeCanExecute();
        reviewDueCommand?.ChangeCanExecute();
    }
}
