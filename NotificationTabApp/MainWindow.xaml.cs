using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Input;
using System.Windows.Media;
using NotificationTabApp.Controls;
using NotificationTabApp.Models;
using NotificationTabApp.Services;
using NotificationTabApp.Views;

namespace NotificationTabApp;

public partial class MainWindow : Window
{
    private readonly AppSettings _settings;
    private readonly SettingsService _settingsService;
    private readonly NotificationScheduler _scheduler;

    // 通知ID -> ポップアップウィンドウ
    private readonly Dictionary<string, NotificationPopup> _activePopups = new();
    // 通知ID -> ユーザーが閉じた発火単位キー
    private readonly Dictionary<string, string> _userClosedTriggerKeys = new();

    private SettingsWindow? _settingsWindow;
    private UpcomingWindow? _upcomingWindow;
    private UpdateWindow? _updateWindow;

    private bool _isPinned = true;
    private bool _isMuted;

    public MainWindow()
    {
        InitializeComponent();

        _settingsService = App.SettingsService;
        _settings = App.Settings;

        // ウィンドウ位置を復元
        var ws = _settings.MainWindow;
        if (ws.X >= 0 && ws.Y >= 0)
        {
            Left = ws.X;
            Top = ws.Y;
        }
        else
        {
            // デフォルト: 画面右上付近
            Left = SystemParameters.PrimaryScreenWidth - Width - 20;
            Top = 20;
        }

        _isPinned = ws.Pinned;
        _isMuted = ws.Muted;
        Topmost = _isPinned;
        UpdatePinButton();
        UpdateMuteButton();

        _scheduler = new NotificationScheduler(() => _settings);
        _scheduler.NotificationFired += OnNotificationFired;
        _scheduler.NotificationEnded += OnNotificationEnded;
        _scheduler.Start();

        LocationChanged += (_, _) => PositionPopups();
        SizeChanged += (_, _) => PositionPopups();
        Closed += MainWindow_Closed;
    }

    // ---- ドラッグ移動 ----
    private void MainBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }

    // ---- 半透明切替 ----
    private void MainBorder_MouseEnter(object sender, MouseEventArgs e)
    {
        MainBorder.Background = new SolidColorBrush(Color.FromArgb(0xCC, 0x22, 0x22, 0x22)); // ~80%
    }

    private void MainBorder_MouseLeave(object sender, MouseEventArgs e)
    {
        MainBorder.Background = new SolidColorBrush(Color.FromArgb(0x80, 0x22, 0x22, 0x22)); // ~50%
    }

    // ---- ピンボタン ----
    private void PinButton_Click(object sender, RoutedEventArgs e)
    {
        _isPinned = !_isPinned;
        Topmost = _isPinned;
        UpdatePinButton();
        SaveWindowState();
    }

    private void UpdatePinButton()
    {
        PinButton.Opacity = _isPinned ? 1.0 : 0.4;
        PinButton.ToolTip = _isPinned ? "常に最前面: ON（クリックでOFF）" : "常に最前面: OFF（クリックでON）";
    }

    // ---- ミュートボタン ----
    private void MuteButton_Click(object sender, RoutedEventArgs e)
    {
        _isMuted = !_isMuted;
        _settings.MainWindow.Muted = _isMuted;
        UpdateMuteButton();

        if (_isMuted)
            CloseAllActivePopups();

        SaveWindowState();
    }

    private void UpdateMuteButton()
    {
        MuteButton.Content = _isMuted ? "🔕" : "🔔";
        MuteButton.Opacity = _isMuted ? 1.0 : 0.65;
        MuteButton.ToolTip = _isMuted ? "通知ミュート: ON（クリックでOFF）" : "通知ミュート: OFF（クリックでON）";
    }

    // ---- プラスボタン ----
    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        OpenSettingsWindow();
    }

    // ---- 歯車ボタン ----
    private void GearButton_Click(object sender, RoutedEventArgs e)
    {
        if (_updateWindow != null && _updateWindow.IsLoaded)
        {
            _updateWindow.Activate();
            return;
        }

        _updateWindow = new UpdateWindow();
        _updateWindow.Show();
    }

    private void UpcomingButton_Click(object sender, RoutedEventArgs e)
    {
        if (_upcomingWindow != null && _upcomingWindow.IsLoaded)
        {
            _upcomingWindow.Activate();
            return;
        }

        _upcomingWindow = new UpcomingWindow(_settings, _settingsService);
        _upcomingWindow.Show();
    }

    public void OpenSettingsWindow(NotificationItem? editItem = null)
    {
        if (_settingsWindow != null && _settingsWindow.IsLoaded)
        {
            _settingsWindow.Activate();
            return;
        }
        _settingsWindow = new SettingsWindow(editItem);
        _settingsWindow.Show();
    }

    // ---- 最小化 ----
    private void Window_StateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            ShowInTaskbar = false;
        }
        else
        {
            ShowInTaskbar = true;
        }
    }

    // 通知発火時に最小化中なら復帰
    private void RestoreIfMinimized()
    {
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
            ShowInTaskbar = true;
        }
    }

    // ---- 通知発火 ----
    private void OnNotificationFired(object? sender, NotificationFiredEventArgs e)
    {
        if (_isMuted || _settings.MainWindow.Muted)
            return;

        var item = e.Item;

        // ×で閉じた通知は同じ発火単位内では再表示しない
        if (_userClosedTriggerKeys.TryGetValue(item.Id, out var closedKey) &&
            closedKey == e.TriggerKey)
            return;

        if (_activePopups.ContainsKey(item.Id))
            return;

        // 通知音
        try { SystemSounds.Asterisk.Play(); } catch { }

        var popup = new NotificationPopup(item);
        popup.UserClosed += (_, notificationId) => OnPopupUserClosed(notificationId, e.TriggerKey);
        _activePopups[item.Id] = popup;

        if (item.IsOneTime)
        {
            item.HasFired = true;
            _settingsService.Save(_settings);
            _upcomingWindow?.Refresh();
        }

        popup.Show();
        PositionPopups();
        Dispatcher.BeginInvoke(PositionPopups, DispatcherPriority.Loaded);
    }

    // ---- 通知終了（自動閉じ）----
    private void OnNotificationEnded(object? sender, NotificationEndEventArgs e)
    {
        if (_activePopups.TryGetValue(e.NotificationId, out var popup))
        {
            _activePopups.Remove(e.NotificationId);
            popup.Close();
            PositionPopups();
        }

        var oneTimeItem = _settings.OneTimeNotifications.FirstOrDefault(i => i.Id == e.NotificationId && i.HasFired);
        if (oneTimeItem != null)
        {
            _settings.OneTimeNotifications.Remove(oneTimeItem);
            _settingsService.Save(_settings);
            _upcomingWindow?.Refresh();
        }
    }

    // ユーザーが×で閉じた
    private void OnPopupUserClosed(string notificationId, string triggerKey)
    {
        _userClosedTriggerKeys[notificationId] = triggerKey;
        _activePopups.Remove(notificationId);
        PositionPopups();
    }

    // ポップアップを通常画面の下に縦並びで配置
    private void PositionPopups()
    {
        var baseX = Left;
        var baseY = Top + ActualHeight + 4;

        foreach (var popup in GetActivePopupsInNotificationOrder())
        {
            popup.Left = baseX;
            popup.Top = baseY;
            var popupHeight = popup.ActualHeight > 0 ? popup.ActualHeight : popup.Height;
            if (double.IsNaN(popupHeight) || popupHeight <= 0)
                popupHeight = 80;
            baseY += popupHeight + 4;
        }
    }

    private IEnumerable<NotificationPopup> GetActivePopupsInNotificationOrder()
    {
        foreach (var item in _settings.Notifications.Concat(_settings.OneTimeNotifications))
        {
            if (_activePopups.TryGetValue(item.Id, out var popup))
                yield return popup;
        }
    }

    // ---- 終了処理 ----
    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        _scheduler.Stop();
        SaveWindowState();

        // 全ポップアップを閉じる
        foreach (var p in _activePopups.Values.ToList())
            p.Close();
    }

    private void CloseAllActivePopups()
    {
        foreach (var popup in _activePopups.Values.ToList())
            popup.Close();

        _activePopups.Clear();
        PositionPopups();
    }

    private void SaveWindowState()
    {
        _settings.MainWindow.X = Left;
        _settings.MainWindow.Y = Top;
        _settings.MainWindow.Pinned = _isPinned;
        _settings.MainWindow.Muted = _isMuted;
        _settingsService.Save(_settings);
    }
}
