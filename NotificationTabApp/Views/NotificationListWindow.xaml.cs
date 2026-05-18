using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using NotificationTabApp.Models;
using NotificationTabApp.Services;

namespace NotificationTabApp.Views
{
    public class NotificationViewModel : INotifyPropertyChanged
    {
        private bool _isChecked;

        public NotificationItem Item { get; }
        public bool IsDeleteMode { get; }

        public bool IsChecked
        {
            get => _isChecked;
            set { _isChecked = value; OnPropertyChanged(); }
        }

        public string DisplayTitle => Item.Title;
        public string TimeModeLabel => Item.TimeMode == "eorzea" ? "ET" : "現実時間";
        public string TimeRange => $"{TimeModeLabel}  {Item.StartTime} - {Item.EndTime}";
        public string CheckLabel => IsDeleteMode ? "削除" : "通知";
        public string CheckBrush => IsDeleteMode ? "#D32F2F" : "#555555";
        public string ItemBackground => IsDeleteMode ? "#FFFFF7F7" : "White";
        public string ItemBorderBrush => IsDeleteMode ? "#EF9A9A" : "#E0E0E0";
        public string MessagePreview => Item.Message.Length > 60
            ? Item.Message[..60] + "..."
            : Item.Message;

        public NotificationViewModel(NotificationItem item, bool isDeleteMode)
        {
            Item = item;
            IsDeleteMode = isDeleteMode;
            _isChecked = isDeleteMode ? false : item.Enabled;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class NotificationGroupViewModel : INotifyPropertyChanged
    {
        private bool _isEnabled;
        private bool _isExpanded = true;

        public string Id { get; }
        public string Name { get; }
        public NotificationGroup? Group { get; }
        public ObservableCollection<NotificationViewModel> Items { get; }
        public bool IsDeleteMode { get; }

        public bool IsEnabled
        {
            get => _isEnabled;
            set { _isEnabled = value; OnPropertyChanged(); }
        }

        public bool IsExpanded
        {
            get => _isExpanded;
            set { _isExpanded = value; OnPropertyChanged(); }
        }

        public Visibility GroupToggleVisibility => Group == null ? Visibility.Collapsed : Visibility.Visible;
        public bool IsGroupToggleEnabled => Group != null && !IsDeleteMode;
        public string Summary => $"{Items.Count}件";

        public NotificationGroupViewModel(
            string id,
            string name,
            NotificationGroup? group,
            IEnumerable<NotificationViewModel> items,
            bool isDeleteMode)
        {
            Id = id;
            Name = name;
            Group = group;
            Items = new ObservableCollection<NotificationViewModel>(items);
            IsDeleteMode = isDeleteMode;
            _isEnabled = group?.Enabled ?? true;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public partial class NotificationListWindow : Window
    {
        private readonly AppSettings _settings;
        private readonly SettingsService _settingsService;
        private bool _isDeleteMode;
        private ObservableCollection<NotificationGroupViewModel> _groups = new();

        public NotificationListWindow()
        {
            InitializeComponent();

            _settings = App.Settings;
            _settingsService = App.SettingsService;

            Loaded += (_, _) => Refresh();
        }

        private void Refresh()
        {
            var groups = new List<NotificationGroupViewModel>();

            var ungroupedItems = _settings.Notifications
                .Where(n => string.IsNullOrWhiteSpace(n.GroupId))
                .Select(n => new NotificationViewModel(n, _isDeleteMode));
            groups.Add(new NotificationGroupViewModel(string.Empty, "無所属", null, ungroupedItems, _isDeleteMode));

            foreach (var group in _settings.NotificationGroups.OrderBy(g => g.Name))
            {
                var items = _settings.Notifications
                    .Where(n => n.GroupId == group.Id)
                    .Select(n => new NotificationViewModel(n, _isDeleteMode));
                groups.Add(new NotificationGroupViewModel(group.Id, group.Name, group, items, _isDeleteMode));
            }

            var knownGroupIds = _settings.NotificationGroups.Select(g => g.Id).ToHashSet();
            var orphanedItems = _settings.Notifications
                .Where(n => !string.IsNullOrWhiteSpace(n.GroupId) && !knownGroupIds.Contains(n.GroupId))
                .Select(n => new NotificationViewModel(n, _isDeleteMode));
            if (orphanedItems.Any())
                groups.Add(new NotificationGroupViewModel("__missing", "不明なグループ", null, orphanedItems, _isDeleteMode));

            _groups = new ObservableCollection<NotificationGroupViewModel>(groups);
            GroupList.ItemsSource = _groups;
            UpdateDeleteModeUI();
        }

        private void UpdateDeleteModeUI()
        {
            if (_isDeleteMode)
            {
                HeaderText.Text = "削除モード - チェックして削除実行を押してください";
                HeaderText.Foreground = Brushes.Red;
                DeleteModeButton.Content = "削除実行";
                CancelDeleteModeButton.Visibility = Visibility.Visible;
            }
            else
            {
                HeaderText.Text = "登録通知一覧";
                HeaderText.Foreground = Brushes.Black;
                DeleteModeButton.Content = "削除";
                CancelDeleteModeButton.Visibility = Visibility.Collapsed;
            }

            DeleteModeButton.Background = new SolidColorBrush(Color.FromRgb(0xE5, 0x39, 0x35));
        }

        private void GroupEnabledCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (_isDeleteMode) return;

            if (sender is CheckBox cb && cb.DataContext is NotificationGroupViewModel vm && vm.Group != null)
            {
                vm.Group.Enabled = cb.IsChecked == true;
                vm.Group.UpdatedAt = System.DateTime.Now.ToString("o");
                _settingsService.Save(_settings);
            }
        }

        private void DeleteModeButton_Click(object sender, RoutedEventArgs e)
        {
            InfoText.Visibility = Visibility.Collapsed;

            if (!_isDeleteMode)
            {
                _isDeleteMode = true;
                Refresh();
                return;
            }

            var targets = _groups.SelectMany(g => g.Items).Where(vm => vm.IsChecked).ToList();

            if (!targets.Any())
            {
                ShowInfo("削除する項目を選択してください。");
                return;
            }

            var titles = string.Join("\n", targets.Select(t => $"- {t.Item.Title}"));
            var result = MessageBox.Show(
                $"削除しますか？\n\n削除対象:\n{titles}",
                "削除の確認",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.OK)
            {
                foreach (var vm in targets)
                    _settings.Notifications.Remove(vm.Item);

                _settingsService.Save(_settings);
                _isDeleteMode = false;
                Refresh();
            }
        }

        private void CancelDeleteModeButton_Click(object sender, RoutedEventArgs e)
        {
            _isDeleteMode = false;
            InfoText.Visibility = Visibility.Collapsed;
            Refresh();
        }

        private void ItemCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb && cb.DataContext is NotificationViewModel vm)
            {
                if (!_isDeleteMode)
                {
                    vm.Item.Enabled = cb.IsChecked == true;
                    _settingsService.Save(_settings);
                }
            }
        }

        private void ItemContent_Click(object sender, MouseButtonEventArgs e)
        {
            if (_isDeleteMode) return;

            if (sender is FrameworkElement fe && fe.DataContext is NotificationViewModel vm)
            {
                var settingsWindow = new SettingsWindow(vm.Item);
                settingsWindow.Closed += (_, _) => Refresh();
                settingsWindow.ShowDialog();
            }
        }

        private void ShowInfo(string message)
        {
            InfoText.Text = message;
            InfoText.Visibility = Visibility.Visible;
        }
    }
}
