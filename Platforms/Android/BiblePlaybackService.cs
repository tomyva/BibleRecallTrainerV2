using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace BibleRecallTrainerV2.Platforms.Android;

[Service(Exported = false, ForegroundServiceType = ForegroundService.TypeMediaPlayback)]
public sealed class BiblePlaybackService : Service
{
    private const string ChannelId = "bible-reading-playback";
    private const int NotificationId = 4801;
    private PowerManager.WakeLock? playbackWakeLock;

    public override IBinder? OnBind(Intent? intent) => null;

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        CreateChannel();
        AcquirePlaybackWakeLock();

        var launchIntent = PackageManager?.GetLaunchIntentForPackage(PackageName!);
        var pendingFlags = PendingIntentFlags.UpdateCurrent;
        if (OperatingSystem.IsAndroidVersionAtLeast(23))
            pendingFlags |= PendingIntentFlags.Immutable;
        var launchPendingIntent = launchIntent is null
            ? null
            : PendingIntent.GetActivity(this, 0, launchIntent, pendingFlags);

        var builder = OperatingSystem.IsAndroidVersionAtLeast(26)
            ? new Notification.Builder(this, ChannelId)
            : new Notification.Builder(this);
        var notification = builder
            .SetContentTitle("Bible Recall Trainer")
            .SetContentText("Reading your chapter aloud")
            .SetSmallIcon(Resource.Mipmap.appicon)
            .SetOngoing(true)
            .SetContentIntent(launchPendingIntent)
            .Build();

        StartForeground(NotificationId, notification);
        return StartCommandResult.NotSticky;
    }

    public override void OnDestroy()
    {
        ReleasePlaybackWakeLock();
        if (OperatingSystem.IsAndroidVersionAtLeast(24))
        {
#pragma warning disable CA1416
            StopForeground(StopForegroundFlags.Remove);
#pragma warning restore CA1416
        }
        else
        {
#pragma warning disable CS0618, CA1422
            StopForeground(true);
#pragma warning restore CS0618, CA1422
        }
        base.OnDestroy();
    }

    private void AcquirePlaybackWakeLock()
    {
        if (playbackWakeLock?.IsHeld == true)
            return;

        var powerManager = (PowerManager?)GetSystemService(PowerService);
        playbackWakeLock = powerManager?.NewWakeLock(
            WakeLockFlags.Partial,
            $"{PackageName}:BibleReading");
        playbackWakeLock?.SetReferenceCounted(false);
        playbackWakeLock?.Acquire();
    }

    private void ReleasePlaybackWakeLock()
    {
        if (playbackWakeLock?.IsHeld == true)
            playbackWakeLock.Release();
        playbackWakeLock?.Dispose();
        playbackWakeLock = null;
    }

    private void CreateChannel()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(26))
            return;

        var manager = (NotificationManager?)GetSystemService(NotificationService);
        manager?.CreateNotificationChannel(new NotificationChannel(
            ChannelId,
            "Bible reading playback",
            NotificationImportance.Low)
        {
            Description = "Keeps Bible narration playing while the app is in the background"
        });
    }
}
