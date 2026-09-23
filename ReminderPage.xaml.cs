using BibleRecallTrainerV2.ViewModels;

namespace BibleRecallTrainerV2;

public partial class ReminderPage : ContentPage
{
    private readonly ReminderViewModel viewModel;

    public ReminderPage(ReminderViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = this.viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        viewModel.Load();
    }
}
