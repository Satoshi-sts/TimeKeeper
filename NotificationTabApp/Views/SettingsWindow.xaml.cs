using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using NotificationTabApp.Models;
using NotificationTabApp.Services;

namespace NotificationTabApp.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly AppSettings _settings;
        private readonly SettingsService _settingsService;
        private readonly NotificationItem? _editItem;
        private readonly List<string> _startTimeOptions = CreateTimeOptions(includeEndOfDay: false);
        private readonly List<string> _endTimeOptions = CreateTimeOptions(includeEndOfDay: true);
        private readonly ObservableCollection<TimeRangeEditorItem> _timeRanges = new();

        private NotificationListWindow? _listWindow;
        private bool _isInitializing;

        public string? EditingNotificationId => _editItem?.Id;

        public SettingsWindow(NotificationItem? editItem = null)
        {
            InitializeComponent();

            _settings = App.Settings;
            _settingsService = App.SettingsService;
            _editItem = editItem;

            _isInitializing = true;
            StartTimeBox.ItemsSource = _startTimeOptions;
            EndTimeBox.ItemsSource = _endTimeOptions;
            TimeRangeList.ItemsSource = _timeRanges;
            RefreshGroupOptions(editItem?.GroupId ?? string.Empty);

            if (editItem != null)
            {
                Title = "編集 - TimeKeeper";
                TitleBox.Text = editItem.Title;
                OneTimeCheckBox.IsChecked = false;
                OneTimeCheckBox.IsEnabled = false;
                EorzeaTimeCheckBox.IsChecked = editItem.TimeMode == "eorzea";
                foreach (var range in GetTimeRanges(editItem))
                    _timeRanges.Add(new TimeRangeEditorItem(range.StartTime, range.EndTime));
                SelectFirstTimeRange();
                MessageBox.Text = editItem.Message;
            }
            else
            {
                OneTimeCheckBox.IsChecked = false;
                EorzeaTimeCheckBox.IsChecked = false;
            }

            _isInitializing = false;
        }

        private void RefreshGroupOptions(string selectedGroupId)
        {
            var groups = new List<GroupOption> { new(string.Empty, "無所属") };
            groups.AddRange(_settings.NotificationGroups
                .OrderBy(g => g.Name)
                .Select(g => new GroupOption(g.Id, g.Name)));

            GroupBox.ItemsSource = groups;
            GroupBox.SelectedValue = groups.Any(g => g.Id == selectedGroupId) ? selectedGroupId : string.Empty;
        }

        private void AddGroupButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Visibility = Visibility.Collapsed;

            var name = NewGroupNameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                ShowError("追加するグループ名を入力してください。");
                return;
            }

            var existing = _settings.NotificationGroups
                .FirstOrDefault(g => string.Equals(g.Name, name, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                RefreshGroupOptions(existing.Id);
                NewGroupNameBox.Clear();
                return;
            }

            var group = new NotificationGroup { Name = name };
            _settings.NotificationGroups.Add(group);
            _settingsService.Save(_settings);

            RefreshGroupOptions(group.Id);
            NewGroupNameBox.Clear();
        }

        private void StartTimeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;
            if (StartTimeBox.SelectedItem is not string startTime) return;
            if (!TryParseStartTime(startTime, out var startMin)) return;

            var endMin = startMin + 1;
            EndTimeBox.SelectedItem = endMin >= 24 * 60
                ? "24:00"
                : new DateTime(2000, 1, 1).AddMinutes(endMin).ToString("HH:mm");
        }

        private void AddTimeRangeButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Visibility = Visibility.Collapsed;

            var startTime = StartTimeBox.SelectedItem as string ?? string.Empty;
            var endTime = EndTimeBox.SelectedItem as string ?? string.Empty;
            if (!ValidateTimeRange(startTime, endTime))
                return;

            if (_timeRanges.Any(r => r.StartTime == startTime && r.EndTime == endTime))
            {
                ShowError("同じ通知時間帯はすでに追加されています。");
                return;
            }

            _timeRanges.Add(new TimeRangeEditorItem(startTime, endTime));
        }

        private void RemoveTimeRangeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is TimeRangeEditorItem range)
                _timeRanges.Remove(range);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Visibility = Visibility.Collapsed;

            var title = TitleBox.Text.Trim();
            var groupId = GroupBox.SelectedValue as string ?? string.Empty;
            var timeRanges = CollectTimeRanges();
            var message = MessageBox.Text.Trim();
            var timeMode = EorzeaTimeCheckBox.IsChecked == true ? "eorzea" : "real";
            var isOneTime = OneTimeCheckBox.IsChecked == true;

            if (string.IsNullOrEmpty(title))
            {
                ShowError("タイトルを入力してください。");
                return;
            }
            if (timeRanges.Count == 0)
                return;
            if (string.IsNullOrWhiteSpace(message))
            {
                ShowError("表示文字を入力してください。");
                return;
            }

            var firstRange = timeRanges[0];
            if (_editItem != null)
            {
                _editItem.Title = title;
                _editItem.GroupId = groupId;
                _editItem.TimeMode = timeMode;
                _editItem.StartTime = firstRange.StartTime;
                _editItem.EndTime = firstRange.EndTime;
                _editItem.TimeRanges = timeRanges;
                _editItem.Message = message;
                _editItem.UpdatedAt = DateTime.Now.ToString("o");
            }
            else if (isOneTime)
            {
                _settings.OneTimeNotifications.Add(new NotificationItem
                {
                    Title = title,
                    GroupId = groupId,
                    TimeMode = timeMode,
                    StartTime = firstRange.StartTime,
                    EndTime = firstRange.EndTime,
                    TimeRanges = timeRanges,
                    Message = message,
                    Enabled = true,
                    IsOneTime = true
                });
            }
            else
            {
                _settings.Notifications.Add(new NotificationItem
                {
                    Title = title,
                    GroupId = groupId,
                    TimeMode = timeMode,
                    StartTime = firstRange.StartTime,
                    EndTime = firstRange.EndTime,
                    TimeRanges = timeRanges,
                    Message = message,
                    Enabled = true
                });
            }

            _settingsService.Save(_settings);
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void NotificationListButton_Click(object sender, RoutedEventArgs e)
        {
            if (_listWindow != null && _listWindow.IsLoaded)
            {
                _listWindow.Activate();
                return;
            }

            _listWindow = new NotificationListWindow();
            _listWindow.Show();
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }

        private List<NotificationTimeRange> CollectTimeRanges()
        {
            if (_timeRanges.Count > 0)
            {
                return _timeRanges
                    .Select(r => new NotificationTimeRange { StartTime = r.StartTime, EndTime = r.EndTime })
                    .ToList();
            }

            var startTime = StartTimeBox.SelectedItem as string ?? string.Empty;
            var endTime = EndTimeBox.SelectedItem as string ?? string.Empty;
            if (!ValidateTimeRange(startTime, endTime))
                return new List<NotificationTimeRange>();

            return new List<NotificationTimeRange>
            {
                new() { StartTime = startTime, EndTime = endTime }
            };
        }

        private bool ValidateTimeRange(string startTime, string endTime)
        {
            if (!TryParseStartTime(startTime, out _))
            {
                ShowError("通知開始時間を選択してください。");
                return false;
            }
            if (!TryParseEndTime(endTime, out _))
            {
                ShowError("通知終了時間を選択してください。");
                return false;
            }
            if (startTime == endTime)
            {
                ShowError("通知開始時間と通知終了時間が同じです。");
                return false;
            }

            return true;
        }

        private void SelectFirstTimeRange()
        {
            if (_timeRanges.Count == 0)
                return;

            StartTimeBox.SelectedItem = _timeRanges[0].StartTime;
            EndTimeBox.SelectedItem = _timeRanges[0].EndTime;
        }

        private static IEnumerable<NotificationTimeRange> GetTimeRanges(NotificationItem item)
        {
            if (item.TimeRanges is { Count: > 0 })
                return item.TimeRanges;

            if (!string.IsNullOrWhiteSpace(item.StartTime) && !string.IsNullOrWhiteSpace(item.EndTime))
                return new[] { new NotificationTimeRange { StartTime = item.StartTime, EndTime = item.EndTime } };

            return Array.Empty<NotificationTimeRange>();
        }

        private static List<string> CreateTimeOptions(bool includeEndOfDay)
        {
            var options = new List<string>(24 * 60 + 1);
            for (var h = 0; h < 24; h++)
            {
                for (var m = 0; m < 60; m++)
                    options.Add($"{h:00}:{m:00}");
            }

            if (includeEndOfDay)
                options.Add("24:00");

            return options;
        }

        private static bool TryParseStartTime(string text, out int minutesOfDay)
            => TryParseHHmm(text, allow24Hour: false, out minutesOfDay);

        private static bool TryParseEndTime(string text, out int minutesOfDay)
            => TryParseHHmm(text, allow24Hour: true, out minutesOfDay);

        private static bool TryParseHHmm(string text, bool allow24Hour, out int minutesOfDay)
        {
            minutesOfDay = 0;
            if (string.IsNullOrEmpty(text)) return false;

            var parts = text.Split(':');
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

        private sealed record GroupOption(string Id, string Name);
        private sealed record TimeRangeEditorItem(string StartTime, string EndTime)
        {
            public string DisplayText => $"{StartTime} - {EndTime}";
        }
    }
}
