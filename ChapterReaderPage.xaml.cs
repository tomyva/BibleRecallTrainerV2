using BibleRecallTrainerV2.ViewModels;

namespace BibleRecallTrainerV2;

public partial class ChapterReaderPage : ContentPage, IQueryAttributable
{
    private readonly ChapterReaderViewModel viewModel;

    public ChapterReaderPage(ChapterReaderViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = this.viewModel = viewModel;
        viewModel.ActiveVerseRequested += OnActiveVerseRequested;
        viewModel.ReadingPositionRequested += OnReadingPositionRequested;
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("bookId", out var bookValue) ||
            !query.TryGetValue("chapter", out var chapterValue) ||
            !int.TryParse(chapterValue?.ToString(), out var chapter))
            return;

        await viewModel.LoadAsync(Uri.UnescapeDataString(bookValue?.ToString() ?? string.Empty), chapter);
    }

    protected override bool OnBackButtonPressed()
    {
        viewModel.StopPlayback();
        return base.OnBackButtonPressed();
    }

    private void OnActiveVerseRequested(object? sender, ReaderVerseViewModel verse) =>
        Dispatcher.Dispatch(() => VersesView.ScrollTo(verse, position: ScrollToPosition.Center, animate: true));

    private void OnReadingPositionRequested(object? sender, ReaderVerseViewModel verse) =>
        Dispatcher.Dispatch(() => VersesView.ScrollTo(verse, position: ScrollToPosition.Start, animate: false));

    private void OnVersesScrolled(object? sender, ItemsViewScrolledEventArgs e) =>
        viewModel.SaveVisiblePosition(e.FirstVisibleItemIndex);
}
