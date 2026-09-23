namespace BibleRecallTrainerV2;

using Microsoft.Extensions.DependencyInjection;

public partial class AppShell : Shell
{
    public AppShell(IServiceProvider services)
    {
        InitializeComponent();
        HomeContent.Content = services.GetRequiredService<MainPage>();
        Routing.RegisterRoute(nameof(BooksPage), new ServiceRouteFactory<BooksPage>(services));
        Routing.RegisterRoute(nameof(ChaptersPage), new ServiceRouteFactory<ChaptersPage>(services));
        Routing.RegisterRoute(nameof(ChapterReaderPage), new ServiceRouteFactory<ChapterReaderPage>(services));
        Routing.RegisterRoute(nameof(RecallPage), new ServiceRouteFactory<RecallPage>(services));
        Routing.RegisterRoute(nameof(QuizPage), new ServiceRouteFactory<QuizPage>(services));
        Routing.RegisterRoute(nameof(StudyPlanPage), new ServiceRouteFactory<StudyPlanPage>(services));
        Routing.RegisterRoute(nameof(ReminderPage), new ServiceRouteFactory<ReminderPage>(services));
        Routing.RegisterRoute(nameof(DataToolsPage), new ServiceRouteFactory<DataToolsPage>(services));
    }
}
