using BibleRecallTrainerV2.ViewModels;

namespace BibleRecallTrainerV2;

public partial class ChaptersPage : ContentPage, IQueryAttributable
{
    private readonly ChaptersViewModel viewModel;

    public ChaptersPage(ChaptersViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = this.viewModel = viewModel;
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("bookId", out var value))
            await viewModel.LoadAsync(Uri.UnescapeDataString(value?.ToString() ?? string.Empty));
    }
}
