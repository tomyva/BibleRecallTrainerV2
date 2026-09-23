#if ANDROID
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using BibleRecallTrainerV2.Platforms.Android;
using Microsoft.Maui.ApplicationModel;
#endif

namespace BibleRecallTrainerV2.Services;

public sealed class LocalReminderScheduler : ILocalReminderScheduler
{
#if ANDROID
    private const int RequestCode = 4701;
    private const int MaximumReminderCount = 8;
    private const string NotificationIdExtra = "notification-id";
    public bool IsSupported => true;

    public async Task<bool> ScheduleAsync(IReadOnlyList<TimeSpan> timesOfDay, bool requestPermission, CancellationToken cancellationToken = default)
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
            if (status != PermissionStatus.Granted && requestPermission)
                status = await Permissions.RequestAsync<Permissions.PostNotifications>();
            if (status != PermissionStatus.Granted)
                return false;
        }

        cancellationToken.ThrowIfCancellationRequested();
        var context = global::Android.App.Application.Context;
        var alarmManager = (AlarmManager?)context.GetSystemService(Context.AlarmService);
        if (alarmManager is null)
            return false;

        CancelAll(context, alarmManager);
        var times = timesOfDay.Distinct().Order().Take(MaximumReminderCount).ToList();
        for (var index = 0; index < times.Count; index++)
        {
            var now = DateTime.Now;
            var next = now.Date.Add(times[index]);
            if (next <= now)
                next = next.AddDays(1);

            alarmManager.SetInexactRepeating(
                AlarmType.RtcWakeup,
                new DateTimeOffset(next).ToUnixTimeMilliseconds(),
                AlarmManager.IntervalDay,
                CreatePendingIntent(context, index));
        }
        return true;
    }

    public Task CancelAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var context = global::Android.App.Application.Context;
        var alarmManager = (AlarmManager?)context.GetSystemService(Context.AlarmService);
        if (alarmManager is not null)
            CancelAll(context, alarmManager);
        return Task.CompletedTask;
    }

    private static void CancelAll(Context context, AlarmManager alarmManager)
    {
        for (var index = 0; index < MaximumReminderCount; index++)
            alarmManager.Cancel(CreatePendingIntent(context, index));
    }

    private static PendingIntent CreatePendingIntent(Context context, int index)
    {
        var intent = new Intent(context, typeof(ReminderAlarmReceiver));
        intent.PutExtra(NotificationIdExtra, RequestCode + index);
        var flags = PendingIntentFlags.UpdateCurrent;
        if (OperatingSystem.IsAndroidVersionAtLeast(23))
            flags |= PendingIntentFlags.Immutable;
        return PendingIntent.GetBroadcast(context, RequestCode + index, intent, flags)!;
    }
#else
    public bool IsSupported => false;
    public Task<bool> ScheduleAsync(IReadOnlyList<TimeSpan> timesOfDay, bool requestPermission, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);
    public Task CancelAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
#endif
}
