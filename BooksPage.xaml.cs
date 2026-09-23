using BibleRecallTrainerV2.ViewModels;

namespace BibleRecallTrainerV2;

public partial class BooksPage : ContentPage
{
    private readonly BooksViewModel viewModel;

    public BooksPage(BooksViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = this.viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await viewModel.LoadAsync();
    }
}
