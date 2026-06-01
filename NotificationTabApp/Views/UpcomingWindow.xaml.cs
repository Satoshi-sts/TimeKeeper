using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using NotificationTabApp.Models;
using NotificationTabApp.Services;

namespace NotificationTabApp.Views
{
    public class UpcomingItemViewModel
    {
        public NotificationItem Item { get; }
        public NotificationTimeRange TimeRange { get; }
        public DateTimeOffset DueAt { get; }
        public string TriggerKey { get; }
        public string Title => Item.Title;
        public string TimeText => $"{(Item.TimeMode == "eorzea" ? "ET" : "現実時間")} {TimeRange.StartTime} - {TimeRange.EndTime}";

        public UpcomingItemViewModel(
            NotificationItem item,
            NotificationTimeRange timeRange,
            DateTimeOffset dueAt,
            string triggerKey)
        {
            Item = item;
            TimeRange = timeRange;
            DueAt = dueAt;
            TriggerKey = triggerKey;
        }
    }

    public partial class UpcomingWindow : Window
    {
        private const double EorzeaMultiplier = 1440.0 / 70.0;

        private readonly AppSettings _settings;
        private readonly SettingsService _settingsService;
        private ObservableCollection<UpcomingItemViewModel> _items = new();

        public UpcomingWindow(AppSettings settings, SettingsService settingsService)
        {
            InitializeComponent();
            _settings = settings;
            _settingsService = settingsService;

            Loaded += (_, _) => Refresh();
        }

        public void Refresh()
        {
            var items = _settings.Notifications
                .Concat(_settings.OneTimeNotifications)
                .Where(IsVisibleCandidate)
                .SelectMany(CreateUpcomingItems)
                .Where(i => !_settings.SkippedOccurrences.Any(s =>
                    s.NotificationId == i.Item.Id && s.TriggerKey == i.TriggerKey))
                .OrderBy(i => i.DueAt)
                .Take(10)
                .ToList();

            _items = new ObservableCollection<UpcomingItemViewModel>(items);
            UpcomingList.ItemsSource = _items;

            InfoText.Text = items.Count == 0 ? "直近の予定はありません。" : string.Empty;
            InfoText.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private bool IsVisibleCandidate(NotificationItem item)
        {
            if (!item.Enabled || item.HasFired)
                return false;

            if (string.IsNullOrWhiteSpace(item.GroupId))
                return true;

            var group = _settings.NotificationGroups.FirstOrDefault(g => g.Id == item.GroupId);
            return group?.Enabled ?? true;
        }

        private static IEnumerable<UpcomingItemViewModel> CreateUpcomingItems(NotificationItem item)
        {
            var ranges = GetTimeRanges(item).ToList();
            var useRangeKey = ranges.Count > 1;

            foreach (var range in ranges)
            {
                if (!TryParseStartTime(range.StartTime, out var startMin))
                    continue;

                yield return item.TimeMode == "eorzea"
                    ? CreateEorzeaUpcomingItem(item, range, startMin, useRangeKey)
                    : CreateRealUpcomingItem(item, range, startMin, useRangeKey);
            }
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

        private static UpcomingItemViewModel CreateRealUpcomingItem(
            NotificationItem item,
            NotificationTimeRange range,
            int startMin,
            bool useRangeKey)
        {
            var now = DateTime.Now;
            var todayStart = now.Date.AddMinutes(startMin);
            var dueAt = todayStart > now ? todayStart : todayStart.AddDays(1);
            var triggerKey = $"real:{dueAt:yyyy-MM-dd}{GetRangeKey(range, useRangeKey)}";
            return new UpcomingItemViewModel(item, range, new DateTimeOffset(dueAt), triggerKey);
        }

        private static UpcomingItemViewModel CreateEorzeaUpcomingItem(
            NotificationItem item,
            NotificationTimeRange range,
            int startMin,
            bool useRangeKey)
        {
            var nowUtc = DateTimeOffset.UtcNow;
            var eorzeaTotalSeconds = nowUtc.ToUnixTimeSeconds() * EorzeaMultiplier;
            var currentDay = (long)Math.Floor(eorzeaTotalSeconds / 86400.0);
            var currentSecondsInDay = (int)(Math.Floor(eorzeaTotalSeconds) % 86400);
            if (currentSecondsInDay < 0)
                currentSecondsInDay += 86400;

            var currentMin = currentSecondsInDay / 60;
            var targetDay = startMin > currentMin ? currentDay : currentDay + 1;
            var targetEorzeaSeconds = targetDay * 86400.0 + startMin * 60.0;
            var targetEarthSeconds = targetEorzeaSeconds / EorzeaMultiplier;
            var dueAt = DateTimeOffset.UnixEpoch.AddSeconds(targetEarthSeconds).ToLocalTime();
            var triggerKey = $"et:{targetDay}{GetRangeKey(range, useRangeKey)}";

            return new UpcomingItemViewModel(item, range, dueAt, triggerKey);
        }

        private static string GetRangeKey(NotificationTimeRange range, bool useRangeKey)
            => useRangeKey
                ? $":{range.StartTime.Replace(":", "")}-{range.EndTime.Replace(":", "")}"
                : string.Empty;

        private void SkipButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement fe || fe.DataContext is not UpcomingItemViewModel vm)
                return;

            var message = vm.Item.IsOneTime
                ? "この一度限りの通知を削除します。"
                : "この通知時間帯をスキップします。同じ通知の他の時間帯には影響しません。";

            var result = MessageBox.Show(
                $"{message}\n\nスキップしますか？",
                "通知のスキップ",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Information);

            if (result != MessageBoxResult.OK)
                return;

            if (vm.Item.IsOneTime)
            {
                _settings.OneTimeNotifications.Remove(vm.Item);
            }
            else if (!_settings.SkippedOccurrences.Any(s =>
                         s.NotificationId == vm.Item.Id && s.TriggerKey == vm.TriggerKey))
            {
                _settings.SkippedOccurrences.Add(new SkippedNotificationOccurrence
                {
                    NotificationId = vm.Item.Id,
                    TriggerKey = vm.TriggerKey
                });
            }

            _settingsService.Save(_settings);
            Refresh();
        }

        private static bool TryParseStartTime(string time, out int minutesOfDay)
        {
            minutesOfDay = 0;
            if (string.IsNullOrEmpty(time)) return false;

            var parts = time.Split(':');
            if (parts.Length != 2) return false;
            if (!int.TryParse(parts[0], out var hours) || !int.TryParse(parts[1], out var minutes))
                return false;

            if (hours < 0 || hours > 23 || minutes < 0 || minutes > 59)
                return false;

            minutesOfDay = hours * 60 + minutes;
            return true;
        }
    }
}
