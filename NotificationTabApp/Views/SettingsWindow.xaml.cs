using System;
using System.Collections.Generic;
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
        private readonly List<string> _timeOptions = CreateTimeOptions();

        private NotificationListWindow? _listWindow;
        private bool _isInitializing;

        public SettingsWindow(NotificationItem? editItem = null)
        {
            InitializeComponent();

            _settings = App.Settings;
            _settingsService = App.SettingsService;
            _editItem = editItem;

            _isInitializing = true;
            StartTimeBox.ItemsSource = _timeOptions;
            EndTimeBox.ItemsSource = _timeOptions;
            RefreshGroupOptions(editItem?.GroupId ?? string.Empty);

            if (editItem != null)
            {
                Title = "編集 - TimeKeeper";
                TitleBox.Text = editItem.Title;
                OneTimeCheckBox.IsChecked = false;
                OneTimeCheckBox.IsEnabled = false;
                EorzeaTimeCheckBox.IsChecked = editItem.TimeMode == "eorzea";
                StartTimeBox.SelectedItem = editItem.StartTime;
                EndTimeBox.SelectedItem = editItem.EndTime;
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
            if (!TryParseHHmm(startTime, out var h, out var m)) return;

            var endDt = new DateTime(2000, 1, 1, h, m, 0).AddMinutes(1);
            EndTimeBox.SelectedItem = endDt.ToString("HH:mm");
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Visibility = Visibility.Collapsed;

            var title = TitleBox.Text.Trim();
            var groupId = GroupBox.SelectedValue as string ?? string.Empty;
            var startTime = StartTimeBox.SelectedItem as string ?? string.Empty;
            var endTime = EndTimeBox.SelectedItem as string ?? string.Empty;
            var message = MessageBox.Text.Trim();
            var timeMode = EorzeaTimeCheckBox.IsChecked == true ? "eorzea" : "real";
            var isOneTime = OneTimeCheckBox.IsChecked == true;

            if (string.IsNullOrEmpty(title))
            {
                ShowError("タイトルを入力してください。");
                return;
            }
            if (!TryParseHHmm(startTime, out _, out _))
            {
                ShowError("通知開始時間を選択してください。");
                return;
            }
            if (!TryParseHHmm(endTime, out _, out _))
            {
                ShowError("通知終了時間を選択してください。");
                return;
            }
            if (startTime == endTime)
            {
                ShowError("通知開始時間と通知終了時間が同じです。");
                return;
            }
            if (string.IsNullOrWhiteSpace(message))
            {
                ShowError("表示文字を入力してください。");
                return;
            }

            if (_editItem != null)
            {
                _editItem.Title = title;
                _editItem.GroupId = groupId;
                _editItem.TimeMode = timeMode;
                _editItem.StartTime = startTime;
                _editItem.EndTime = endTime;
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
                    StartTime = startTime,
                    EndTime = endTime,
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
                    StartTime = startTime,
                    EndTime = endTime,
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

        private static List<string> CreateTimeOptions()
        {
            var options = new List<string>(24 * 60);
            for (var h = 0; h < 24; h++)
            {
                for (var m = 0; m < 60; m++)
                    options.Add($"{h:00}:{m:00}");
            }
            return options;
        }

        private static bool TryParseHHmm(string text, out int hours, out int minutes)
        {
            hours = 0;
            minutes = 0;
            if (string.IsNullOrEmpty(text)) return false;

            var parts = text.Split(':');
            if (parts.Length != 2) return false;
            if (!int.TryParse(parts[0], out hours) || !int.TryParse(parts[1], out minutes))
                return false;

            return hours >= 0 && hours <= 23 && minutes >= 0 && minutes <= 59;
        }

        private sealed record GroupOption(string Id, string Name);
    }
}
