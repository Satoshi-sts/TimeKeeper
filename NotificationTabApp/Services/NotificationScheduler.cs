using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;
using NotificationTabApp.Models;

namespace NotificationTabApp.Services
{
    public class NotificationFiredEventArgs : EventArgs
    {
        public NotificationItem Item { get; }
        public string TriggerKey { get; }

        public NotificationFiredEventArgs(NotificationItem item, string triggerKey)
        {
            Item = item;
            TriggerKey = triggerKey;
        }
    }

    public class NotificationEndEventArgs : EventArgs
    {
        public string NotificationId { get; }
        public NotificationEndEventArgs(string id) => NotificationId = id;
    }

    public class NotificationScheduler
    {
        private const string RealTimeMode = "real";
        private const string EorzeaTimeMode = "eorzea";
        private const double EorzeaMultiplier = 1440.0 / 70.0;

        private readonly DispatcherTimer _timer;
        private readonly Func<AppSettings> _getSettings;
        private readonly Dictionary<string, string> _firedKeys = new();

        public event EventHandler<NotificationFiredEventArgs>? NotificationFired;
        public event EventHandler<NotificationEndEventArgs>? NotificationEnded;

        public NotificationScheduler(Func<AppSettings> getSettings)
        {
            _getSettings = getSettings;
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(10)
            };
            _timer.Tick += OnTick;
        }

        public void Start()
        {
            _timer.Start();
            OnTick(this, EventArgs.Empty);
        }

        public void Stop() => _timer.Stop();

        private void OnTick(object? sender, EventArgs e)
        {
            var now = DateTime.Now;
            var nowUtc = DateTimeOffset.UtcNow;
            var eorzeaSnapshot = GetEorzeaSnapshot(nowUtc);
            var settings = _getSettings();

            if (settings.MainWindow.Muted)
            {
                EndAllActiveNotifications();
                return;
            }

            foreach (var item in settings.Notifications.Concat(settings.OneTimeNotifications))
            {
                if (item.IsOneTime && item.HasFired && !_firedKeys.ContainsKey(item.Id))
                    continue;

                if (!item.Enabled || !IsGroupEnabled(settings, item))
                {
                    if (_firedKeys.Remove(item.Id))
                        NotificationEnded?.Invoke(this, new NotificationEndEventArgs(item.Id));
                    continue;
                }

                if (!TryParseTime(item.StartTime, out var startH, out var startM)) continue;
                if (!TryParseTime(item.EndTime, out var endH, out var endM)) continue;

                var startMin = startH * 60 + startM;
                var endMin = endH * 60 + endM;
                var mode = NormalizeTimeMode(item.TimeMode);
                var nowMin = GetCurrentMinute(mode, now, eorzeaSnapshot);
                var triggerKey = GetTriggerKey(mode, now, eorzeaSnapshot);
                var inRange = IsInRange(nowMin, startMin, endMin);
                var isSkipped = settings.SkippedOccurrences.Any(s =>
                    s.NotificationId == item.Id && s.TriggerKey == triggerKey);

                if (isSkipped && !_firedKeys.ContainsKey(item.Id))
                    continue;

                if (!(_firedKeys.TryGetValue(item.Id, out var firedKey) && firedKey == triggerKey))
                {
                    if (inRange)
                    {
                        _firedKeys[item.Id] = triggerKey;
                        NotificationFired?.Invoke(this, new NotificationFiredEventArgs(item, triggerKey));
                    }
                }
                else if (!inRange)
                {
                    NotificationEnded?.Invoke(this, new NotificationEndEventArgs(item.Id));
                }
            }
        }

        private void EndAllActiveNotifications()
        {
            foreach (var notificationId in _firedKeys.Keys.ToList())
            {
                _firedKeys.Remove(notificationId);
                NotificationEnded?.Invoke(this, new NotificationEndEventArgs(notificationId));
            }
        }

        private static int GetCurrentMinute(string mode, DateTime now, EorzeaTimeSnapshot eorzeaSnapshot)
        {
            if (mode == EorzeaTimeMode)
                return eorzeaSnapshot.Hour * 60 + eorzeaSnapshot.Minute;

            return now.Hour * 60 + now.Minute;
        }

        private static string GetTriggerKey(string mode, DateTime now, EorzeaTimeSnapshot eorzeaSnapshot)
        {
            if (mode == EorzeaTimeMode)
                return $"et:{eorzeaSnapshot.DayNumber}";

            return $"real:{now:yyyy-MM-dd}";
        }

        private static EorzeaTimeSnapshot GetEorzeaSnapshot(DateTimeOffset nowUtc)
        {
            var earthUnixSeconds = nowUtc.ToUnixTimeSeconds();
            var eorzeaTotalSeconds = earthUnixSeconds * EorzeaMultiplier;
            var dayNumber = (long)Math.Floor(eorzeaTotalSeconds / 86400.0);
            var secondsInDay = (int)(Math.Floor(eorzeaTotalSeconds) % 86400);

            if (secondsInDay < 0)
                secondsInDay += 86400;

            return new EorzeaTimeSnapshot(
                dayNumber,
                secondsInDay / 3600,
                (secondsInDay % 3600) / 60);
        }

        private static string NormalizeTimeMode(string? mode)
            => string.Equals(mode, EorzeaTimeMode, StringComparison.OrdinalIgnoreCase)
                ? EorzeaTimeMode
                : RealTimeMode;

        private static bool IsGroupEnabled(AppSettings settings, NotificationItem item)
        {
            if (string.IsNullOrWhiteSpace(item.GroupId))
                return true;

            var group = settings.NotificationGroups.FirstOrDefault(g => g.Id == item.GroupId);
            return group?.Enabled ?? true;
        }

        private static bool IsInRange(int nowMin, int startMin, int endMin)
        {
            if (startMin < endMin)
                return nowMin >= startMin && nowMin < endMin;

            return nowMin >= startMin || nowMin < endMin;
        }

        private static bool TryParseTime(string time, out int hours, out int minutes)
        {
            hours = 0;
            minutes = 0;
            if (string.IsNullOrEmpty(time)) return false;

            var parts = time.Split(':');
            if (parts.Length != 2) return false;
            if (!int.TryParse(parts[0], out hours) || !int.TryParse(parts[1], out minutes))
                return false;

            return hours >= 0 && hours <= 23 && minutes >= 0 && minutes <= 59;
        }

        private readonly record struct EorzeaTimeSnapshot(long DayNumber, int Hour, int Minute);
    }
}
