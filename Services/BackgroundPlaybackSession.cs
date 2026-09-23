#if ANDROID
using Android.Content;
using BibleRecallTrainerV2.Platforms.Android;
#endif

namespace BibleRecallTrainerV2.Services;

public sealed class BackgroundPlaybackSession : IBackgroundPlaybackSession
{
    public void Begin()
    {
#if ANDROID
        var context = global::Android.App.Application.Context;
        var intent = new Intent(context, typeof(BiblePlaybackService));
        if (OperatingSystem.IsAndroidVersionAtLeast(26))
            context.StartForegroundService(intent);
        else
            context.StartService(intent);
#endif
    }

    public void End()
    {
#if ANDROID
        var context = global::Android.App.Application.Context;
        context.StopService(new Intent(context, typeof(BiblePlaybackService)));
#endif
    }
}
