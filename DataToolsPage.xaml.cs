using BibleRecallTrainerV2.ViewModels;

namespace BibleRecallTrainerV2;

public partial class DataToolsPage : ContentPage
{
    private readonly DataToolsViewModel viewModel;

    public DataToolsPage(DataToolsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = this.viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await viewModel.LoadAsync();
    }

    private async void OnResetReadingPositionsClicked(object? sender, EventArgs e)
    {
        var confirmed = await DisplayAlertAsync(
            "Reset reading positions?",
            "This will clear the saved verse for every chapter. Your study progress and quiz results will remain.",
            "Reset",
            "Cancel");
        if (confirmed)
            viewModel.ResetReadingPositions();
    }
}
