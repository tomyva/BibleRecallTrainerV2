using BibleRecallTrainerV2.Services;
using BibleRecallTrainerV2.ViewModels;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;

namespace BibleRecallTrainerV2;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSingleton<IBibleAssetReader, MauiBibleAssetReader>();
        builder.Services.AddSingleton<IBibleContentService, LocalBibleContentService>();
        builder.Services.AddSingleton<ITextToSpeech>(_ => TextToSpeech.Default);
        builder.Services.AddSingleton<IPreferences>(_ => Preferences.Default);
        builder.Services.AddSingleton<IAppThemeService, AppThemeService>();
        builder.Services.AddSingleton<ISpeechService, MauiSpeechService>();
        builder.Services.AddSingleton<IBackgroundPlaybackSession, BackgroundPlaybackSession>();
        builder.Services.AddSingleton<ISpeechSettingsService, SpeechSettingsService>();
        builder.Services.AddSingleton<IChapterSpeechPlayer, ChapterSpeechPlayer>();
        builder.Services.AddSingleton<CommunityToolkit.Maui.Media.ISpeechToText>(_ => CommunityToolkit.Maui.Media.SpeechToText.Default);
        builder.Services.AddSingleton<ISpeechRecognitionService, ToolkitSpeechRecognitionService>();
        builder.Services.AddSingleton<IRecallComparisonService, RecallComparisonService>();
        builder.Services.AddSingleton<IQuizService, QuizService>();
        builder.Services.AddSingleton<ILocalDataStore, MauiLocalDataStore>();
        builder.Services.AddSingleton<IProgressService, LocalProgressService>();
        builder.Services.AddSingleton<IReadingPositionStore, ReadingPositionStore>();
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<ISpacedRepetitionService, LocalSpacedRepetitionService>();
        builder.Services.AddSingleton<IStudyPlanSettingsStore, StudyPlanSettingsStore>();
        builder.Services.AddSingleton<IStudyPlanService, StudyPlanService>();
        builder.Services.AddSingleton<IReminderSettingsStore, ReminderSettingsStore>();
        builder.Services.AddSingleton<ILocalReminderScheduler, LocalReminderScheduler>();
        builder.Services.AddSingleton<IReminderService, ReminderService>();
        builder.Services.AddSingleton<IDataHealthService, DataHealthService>();
        builder.Services.AddSingleton<IProgressExportService, ProgressExportService>();
        builder.Services.AddSingleton<IExportShareService, MauiExportShareService>();
        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<StudyPlanViewModel>();
        builder.Services.AddTransient<ReminderViewModel>();
        builder.Services.AddTransient<DataToolsViewModel>();
        builder.Services.AddTransient<BooksViewModel>();
        builder.Services.AddTransient<ChaptersViewModel>();
        builder.Services.AddTransient<ChapterReaderViewModel>();
        builder.Services.AddTransient<RecallViewModel>();
        builder.Services.AddTransient<QuizViewModel>();
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<StudyPlanPage>();
        builder.Services.AddTransient<ReminderPage>();
        builder.Services.AddTransient<DataToolsPage>();
        builder.Services.AddTransient<BooksPage>();
        builder.Services.AddTransient<ChaptersPage>();
        builder.Services.AddTransient<ChapterReaderPage>();
        builder.Services.AddTransient<RecallPage>();
        builder.Services.AddTransient<QuizPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
