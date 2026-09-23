using BibleRecallTrainerV2.ViewModels;

namespace BibleRecallTrainerV2;

public partial class StudyPlanPage : ContentPage
{
    private readonly StudyPlanViewModel viewModel;

    public StudyPlanPage(StudyPlanViewModel viewModel)
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
