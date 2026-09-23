using BibleRecallTrainerV2.Services;

namespace BibleRecallTrainerV2;

public partial class App : Application
{
    private readonly IServiceProvider services;
    private readonly IChapterSpeechPlayer speechPlayer;
    private readonly ISpeechRecognitionService recognitionService;
    private readonly IReminderService reminderService;

    public App(
        IServiceProvider services,
        IChapterSpeechPlayer speechPlayer,
        ISpeechRecognitionService recognitionService,
        IReminderService reminderService,
        IAppThemeService themeService)
    {
        this.services = services;
        this.speechPlayer = speechPlayer;
        this.recognitionService = recognitionService;
        this.reminderService = reminderService;
        InitializeComponent();
        themeService.Initialize(Resources);
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell(services));
        window.Deactivated += OnWindowDeactivated;
        window.Stopped += OnWindowStopped;
        window.Destroying += OnWindowDestroying;
        window.Created += OnWindowCreated;
        return window;
    }

    private async void OnWindowCreated(object? sender, EventArgs e)
    {
        try
        {
            await reminderService.EnsureScheduledAsync();
        }
        catch
        {
            // A reminder failure must never prevent the app from opening.
        }
    }

    private void OnWindowDeactivated(object? sender, EventArgs e) => recognitionService.Cancel();
    private void OnWindowStopped(object? sender, EventArgs e) => recognitionService.Cancel();
    private void OnWindowDestroying(object? sender, EventArgs e) => StopMediaSessions();

    private void StopMediaSessions()
    {
        speechPlayer.Stop();
        recognitionService.Cancel();
    }
}
