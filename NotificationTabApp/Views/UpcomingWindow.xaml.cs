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
        public DateTimeOffset DueAt { get; }
        public string TriggerKey { get; }
        public string Title => Item.Title;
        public string TimeText => $"{(Item.TimeMode == "eorzea" ? "ET" : "現実時間")} {Item.StartTime} - {Item.EndTime}";

        public UpcomingItemViewModel(NotificationItem item, DateTimeOffset dueAt, string triggerKey)
        {
            Item = item;
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
                .Select(CreateUpcomingItem)
                .Where(i => i != null)
                .Cast<UpcomingItemViewModel>()
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

        private static UpcomingItemViewModel? CreateUpcomingItem(NotificationItem item)
        {
            if (!TryParseTime(item.StartTime, out var startH, out var startM))
                return null;

            var startMin = startH * 60 + startM;
            if (item.TimeMode == "eorzea")
                return CreateEorzeaUpcomingItem(item, startMin);

            return CreateRealUpcomingItem(item, startMin);
        }

        private static UpcomingItemViewModel CreateRealUpcomingItem(NotificationItem item, int startMin)
        {
            var now = DateTime.Now;
            var todayStart = now.Date.AddMinutes(startMin);
            var dueAt = todayStart > now ? todayStart : todayStart.AddDays(1);
            var triggerKey = $"real:{dueAt:yyyy-MM-dd}";
            return new UpcomingItemViewModel(item, new DateTimeOffset(dueAt), triggerKey);
        }

        private static UpcomingItemViewModel CreateEorzeaUpcomingItem(NotificationItem item, int startMin)
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
            var triggerKey = $"et:{targetDay}";

            return new UpcomingItemViewModel(item, dueAt, triggerKey);
        }

        private void SkipButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement fe || fe.DataContext is not UpcomingItemViewModel vm)
                return;

            var message = vm.Item.IsOneTime
                ? "この一度限りの通知を削除します。"
                : "この通知をスキップします。ただし、繰り返し通知するものに関しては以降の通知は通常通り通知されます。";

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
    }
}
