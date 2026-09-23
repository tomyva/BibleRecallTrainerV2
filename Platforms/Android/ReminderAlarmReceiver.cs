using Android.App;
using Android.Content;
using Android.OS;

namespace BibleRecallTrainerV2.Platforms.Android;

[BroadcastReceiver(Enabled = true, Exported = false)]
public sealed class ReminderAlarmReceiver : BroadcastReceiver
{
    private const string ChannelId = "bible-study-reminders";
    private const int DefaultNotificationId = 4701;
    private const string NotificationIdExtra = "notification-id";

    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context is null)
            return;

        var manager = (NotificationManager?)context.GetSystemService(Context.NotificationService);
        if (manager is null)
            return;

        if (OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            var channel = new NotificationChannel(ChannelId, "Study reminders", NotificationImportance.Default)
            {
                Description = "Daily reminders for your Bible study plan"
            };
            manager.CreateNotificationChannel(channel);
        }

        var launchIntent = context.PackageManager?.GetLaunchIntentForPackage(context.PackageName!);
        var pendingFlags = PendingIntentFlags.UpdateCurrent;
        if (OperatingSystem.IsAndroidVersionAtLeast(23))
            pendingFlags |= PendingIntentFlags.Immutable;
        var launchPendingIntent = launchIntent is null
            ? null
            : PendingIntent.GetActivity(context, 0, launchIntent, pendingFlags);
        var builder = OperatingSystem.IsAndroidVersionAtLeast(26)
            ? new Notification.Builder(context, ChannelId)
            : new Notification.Builder(context);
        var notification = builder
            .SetContentTitle("Time for Bible recall practice")
            .SetContentText("Continue your chapter plan or review a due passage.")
            .SetSmallIcon(global::Android.Resource.Drawable.IcDialogInfo)
            .SetAutoCancel(true)
            .SetContentIntent(launchPendingIntent)
            .Build();
        manager.Notify(intent?.GetIntExtra(NotificationIdExtra, DefaultNotificationId) ?? DefaultNotificationId, notification);
    }
}
