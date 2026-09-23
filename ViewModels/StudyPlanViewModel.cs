using System.Collections.ObjectModel;
using System.Windows.Input;
using BibleRecallTrainerV2.Models;
using BibleRecallTrainerV2.Services;

namespace BibleRecallTrainerV2.ViewModels;

public sealed class StudyPlanViewModel(IStudyPlanService studyPlanService) : ViewModelBase
{
    private int dailyChapterGoal = 1;
    private string summary = string.Empty;

    public int DailyChapterGoal
    {
        get => dailyChapterGoal;
        set
        {
            var goal = Math.Clamp(value, 1, 5);
            if (!SetProperty(ref dailyChapterGoal, goal))
                return;
            studyPlanService.SetDailyChapterGoal(goal);
            OnPropertyChanged(nameof(GoalText));
        }
    }

    public string GoalText => $"{DailyChapterGoal} chapter{(DailyChapterGoal == 1 ? string.Empty : "s")} per day";

    public string Summary
    {
        get => summary;
        private set => SetProperty(ref summary, value);
    }

    public ObservableCollection<StudyChapterItem> DueReviews { get; } = [];
    public ICommand OpenReviewCommand { get; } = new Command<StudyChapterItem>(async chapter =>
    {
        if (chapter is not null)
            await Shell.Current.GoToAsync($"{nameof(QuizPage)}?bookId={Uri.EscapeDataString(chapter.BookId)}&chapter={chapter.ChapterNumber}");
    });

    public async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var snapshot = await studyPlanService.GetSnapshotAsync();
            dailyChapterGoal = snapshot.DailyChapterGoal;
            OnPropertyChanged(nameof(DailyChapterGoal));
            OnPropertyChanged(nameof(GoalText));
            Summary = $"{snapshot.DailyProgress}. {snapshot.SyllabusProgress}.";
            DueReviews.Clear();
            foreach (var review in snapshot.DueReviews)
                DueReviews.Add(review);
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
}
