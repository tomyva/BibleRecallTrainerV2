using BibleRecallTrainerV2.ViewModels;

namespace BibleRecallTrainerV2;

public partial class RecallPage : ContentPage, IQueryAttributable
{
    private readonly RecallViewModel viewModel;

    public RecallPage(RecallViewModel viewModel)
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

    protected override void OnDisappearing()
    {
        viewModel.CancelSessions();
        base.OnDisappearing();
    }
}
