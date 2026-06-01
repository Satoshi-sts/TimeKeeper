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
        public NotificationTimeRange TimeRange { get; }

        public NotificationFiredEventArgs(NotificationItem item, string triggerKey, NotificationTimeRange timeRange)
        {
            Item = item;
            TriggerKey = triggerKey;
            TimeRange = timeRange;
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

                var mode = NormalizeTimeMode(item.TimeMode);
                var nowMin = GetCurrentMinute(mode, now, eorzeaSnapshot);
                var activeOccurrence = GetActiveOccurrence(item, mode, nowMin, now, eorzeaSnapshot);
                var hasFired = _firedKeys.TryGetValue(item.Id, out var firedKey);

                if (activeOccurrence == null)
                {
                    if (hasFired)
                    {
                        _firedKeys.Remove(item.Id);
                        NotificationEnded?.Invoke(this, new NotificationEndEventArgs(item.Id));
                    }

                    continue;
                }

                var isSkipped = settings.SkippedOccurrences.Any(s =>
                    s.NotificationId == item.Id && s.TriggerKey == activeOccurrence.TriggerKey);

                if (isSkipped && (!hasFired || firedKey != activeOccurrence.TriggerKey))
                {
                    if (hasFired)
                    {
                        _firedKeys.Remove(item.Id);
                        NotificationEnded?.Invoke(this, new NotificationEndEventArgs(item.Id));
                    }

                    continue;
                }

                if (hasFired && firedKey == activeOccurrence.TriggerKey)
                    continue;

                if (hasFired)
                {
                    _firedKeys.Remove(item.Id);
                    NotificationEnded?.Invoke(this, new NotificationEndEventArgs(item.Id));
                }

                _firedKeys[item.Id] = activeOccurrence.TriggerKey;
                NotificationFired?.Invoke(this, new NotificationFiredEventArgs(
                    item,
                    activeOccurrence.TriggerKey,
                    activeOccurrence.TimeRange));
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

        private static ActiveOccurrence? GetActiveOccurrence(
            NotificationItem item,
            string mode,
            int nowMin,
            DateTime now,
            EorzeaTimeSnapshot eorzeaSnapshot)
        {
            var ranges = GetTimeRanges(item).ToList();
            var useRangeKey = ranges.Count > 1;

            foreach (var range in ranges)
            {
                if (!TryParseStartTime(range.StartTime, out var startMin)) continue;
                if (!TryParseEndTime(range.EndTime, out var endMin)) continue;
                if (startMin == endMin) continue;
                if (!IsInRange(nowMin, startMin, endMin)) continue;

                return new ActiveOccurrence(
                    range,
                    GetTriggerKey(mode, now, eorzeaSnapshot, nowMin, startMin, endMin, range, useRangeKey));
            }

            return null;
        }

        private static IEnumerable<NotificationTimeRange> GetTimeRanges(NotificationItem item)
        {
            if (item.TimeRanges is { Count: > 0 })
                return item.TimeRanges;

            return new[]
            {
                new NotificationTimeRange
                {
                    StartTime = item.StartTime,
                    EndTime = item.EndTime
                }
            };
        }

        private static string GetTriggerKey(
            string mode,
            DateTime now,
            EorzeaTimeSnapshot eorzeaSnapshot,
            int nowMin,
            int startMin,
            int endMin,
            NotificationTimeRange range,
            bool useRangeKey)
        {
            var rangeKey = useRangeKey
                ? $":{range.StartTime.Replace(":", "")}-{range.EndTime.Replace(":", "")}"
                : string.Empty;

            if (mode == EorzeaTimeMode)
            {
                var dayNumber = GetOccurrenceDayNumber(eorzeaSnapshot.DayNumber, nowMin, startMin, endMin);
                return $"et:{dayNumber}{rangeKey}";
            }

            var date = GetOccurrenceDate(now.Date, nowMin, startMin, endMin);
            return $"real:{date:yyyy-MM-dd}{rangeKey}";
        }

        private static DateTime GetOccurrenceDate(DateTime currentDate, int nowMin, int startMin, int endMin)
        {
            if (CrossesMidnight(startMin, endMin) && nowMin < endMin)
                return currentDate.AddDays(-1);

            return currentDate;
        }

        private static long GetOccurrenceDayNumber(long currentDayNumber, int nowMin, int startMin, int endMin)
        {
            if (CrossesMidnight(startMin, endMin) && nowMin < endMin)
                return currentDayNumber - 1;

            return currentDayNumber;
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

        private static bool CrossesMidnight(int startMin, int endMin)
            => endMin <= startMin;

        private static bool TryParseStartTime(string time, out int minutesOfDay)
            => TryParseTime(time, allow24Hour: false, out minutesOfDay);

        private static bool TryParseEndTime(string time, out int minutesOfDay)
            => TryParseTime(time, allow24Hour: true, out minutesOfDay);

        private static bool TryParseTime(string time, bool allow24Hour, out int minutesOfDay)
        {
            minutesOfDay = 0;
            if (string.IsNullOrEmpty(time)) return false;

            var parts = time.Split(':');
            if (parts.Length != 2) return false;
            if (!int.TryParse(parts[0], out var hours) || !int.TryParse(parts[1], out var minutes))
                return false;

            if (allow24Hour && hours == 24 && minutes == 0)
            {
                minutesOfDay = 24 * 60;
                return true;
            }

            if (hours < 0 || hours > 23 || minutes < 0 || minutes > 59)
                return false;

            minutesOfDay = hours * 60 + minutes;
            return true;
        }

        private sealed record ActiveOccurrence(NotificationTimeRange TimeRange, string TriggerKey);
        private readonly record struct EorzeaTimeSnapshot(long DayNumber, int Hour, int Minute);
    }
}
