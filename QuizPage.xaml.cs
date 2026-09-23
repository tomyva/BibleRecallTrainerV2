using BibleRecallTrainerV2.ViewModels;

namespace BibleRecallTrainerV2;

public partial class QuizPage : ContentPage, IQueryAttributable
{
    private readonly QuizViewModel viewModel;

    public QuizPage(QuizViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = this.viewModel = viewModel;
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("bookId", out var bookValue) ||
            !query.TryGetValue("chapter", out var chapterValue) ||
            !int.TryParse(chapterValue?.ToString(), out var chapter))
            return;
        await viewModel.LoadAsync(Uri.UnescapeDataString(bookValue?.ToString() ?? string.Empty), chapter);
    }
}
