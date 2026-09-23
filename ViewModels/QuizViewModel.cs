using System.Windows.Input;
using BibleRecallTrainerV2.Models;
using BibleRecallTrainerV2.Services;

namespace BibleRecallTrainerV2.ViewModels;

public sealed class QuizViewModel : ViewModelBase
{
    private readonly IBibleContentService bibleContent;
    private readonly IQuizService quizService;
    private readonly IProgressService progressService;
    private readonly ISpacedRepetitionService spacedRepetitionService;
    private IReadOnlyList<PracticeQuestion> questions = [];
    private int currentIndex;
    private int correctAnswers;
    private string answer = string.Empty;
    private string feedback = string.Empty;
    private bool isAnswered;
    private bool isComplete;
    private string bookId = string.Empty;
    private int chapterNumber;

    public QuizViewModel(
        IBibleContentService bibleContent,
        IQuizService quizService,
        IProgressService progressService,
        ISpacedRepetitionService spacedRepetitionService)
    {
        this.bibleContent = bibleContent;
        this.quizService = quizService;
        this.progressService = progressService;
        this.spacedRepetitionService = spacedRepetitionService;
        SubmitCommand = new Command(async () => await SubmitAsync(), () => CanSubmit);
        NextCommand = new Command(Next, () => IsAnswered && !IsComplete);
        RestartCommand = new Command(Restart, () => questions.Count > 0);
        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
    }

    public PracticeQuestion? CurrentQuestion => currentIndex < questions.Count ? questions[currentIndex] : null;
    public string Heading => CurrentQuestion is null ? "Chapter Quiz" : $"{CurrentQuestion.BookName} {CurrentQuestion.ChapterNumber}";
    public string Reference => CurrentQuestion is null ? string.Empty : $"Verse {CurrentQuestion.VerseNumber}";
    public string Prompt => CurrentQuestion?.Prompt ?? string.Empty;
    public string Progress => questions.Count == 0 ? string.Empty : $"Question {Math.Min(currentIndex + 1, questions.Count)} of {questions.Count}";
    public string Score => questions.Count == 0 ? string.Empty : $"Score: {correctAnswers}/{questions.Count}";
    public bool CanSubmit => !IsAnswered && !IsComplete && CurrentQuestion is not null && !string.IsNullOrWhiteSpace(Answer);

    public string Answer
    {
        get => answer;
        set
        {
            if (SetProperty(ref answer, value))
            {
                OnPropertyChanged(nameof(CanSubmit));
                ((Command)SubmitCommand).ChangeCanExecute();
            }
        }
    }

    public string Feedback
    {
        get => feedback;
        private set => SetProperty(ref feedback, value);
    }

    public bool IsAnswered
    {
        get => isAnswered;
        private set
        {
            if (!SetProperty(ref isAnswered, value))
                return;
            OnPropertyChanged(nameof(CanSubmit));
            RefreshCommands();
        }
    }

    public bool IsComplete
    {
        get => isComplete;
        private set
        {
            if (!SetProperty(ref isComplete, value))
                return;
            OnPropertyChanged(nameof(CanSubmit));
            RefreshCommands();
        }
    }

    public ICommand SubmitCommand { get; }
    public ICommand NextCommand { get; }
    public ICommand RestartCommand { get; }
    public ICommand BackCommand { get; }

    public async Task LoadAsync(string bookId, int chapterNumber)
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var books = await bibleContent.GetBooksAsync();
            var book = books.FirstOrDefault(item => string.Equals(item.Id, bookId, StringComparison.OrdinalIgnoreCase))
                ?? throw new BibleContentException($"Book '{bookId}' is not included in the syllabus.");
            var chapter = await bibleContent.GetChapterAsync(book.Id, chapterNumber);
            this.bookId = book.Id;
            this.chapterNumber = chapterNumber;
            questions = quizService.CreateChapterQuiz(book, chapter);
            Restart();
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
            questions = [];
            NotifyQuestionChanged();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SubmitAsync()
    {
        if (!CanSubmit || CurrentQuestion is null)
            return;
        var result = quizService.CheckAnswer(CurrentQuestion, Answer);
        if (result.IsCorrect)
            correctAnswers++;
        Feedback = result.Feedback;
        IsAnswered = true;
        OnPropertyChanged(nameof(Score));

        if (currentIndex == questions.Count - 1)
        {
            IsComplete = true;
            var percentage = questions.Count == 0 ? 0 : correctAnswers * 100 / questions.Count;
            Feedback = $"Quiz complete. {Score} ({percentage}%).";
            try
            {
                await progressService.RecordQuizCompletedAsync(bookId, chapterNumber, percentage);
                await spacedRepetitionService.RecordResultAsync(bookId, chapterNumber, percentage);
            }
            catch (ProgressDataException exception)
            {
                ErrorMessage = $"Quiz completed, but progress could not be saved: {exception.Message}";
            }
        }
    }

    private void Next()
    {
        if (!IsAnswered || IsComplete)
            return;
        currentIndex++;
        Answer = string.Empty;
        Feedback = string.Empty;
        IsAnswered = false;
        NotifyQuestionChanged();
    }

    private void Restart()
    {
        currentIndex = 0;
        correctAnswers = 0;
        Answer = string.Empty;
        Feedback = string.Empty;
        IsComplete = false;
        IsAnswered = false;
        NotifyQuestionChanged();
    }

    private void NotifyQuestionChanged()
    {
        OnPropertyChanged(nameof(CurrentQuestion));
        OnPropertyChanged(nameof(Heading));
        OnPropertyChanged(nameof(Reference));
        OnPropertyChanged(nameof(Prompt));
        OnPropertyChanged(nameof(Progress));
        OnPropertyChanged(nameof(Score));
        OnPropertyChanged(nameof(CanSubmit));
        RefreshCommands();
    }

    private void RefreshCommands()
    {
        ((Command)SubmitCommand).ChangeCanExecute();
        ((Command)NextCommand).ChangeCanExecute();
        ((Command)RestartCommand).ChangeCanExecute();
    }
}
