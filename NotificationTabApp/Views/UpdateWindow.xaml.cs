using System.Windows;
using NotificationTabApp.Services;

namespace NotificationTabApp.Views;

public partial class UpdateWindow : Window
{
    private readonly AppUpdateService _updateService = new();
    private AppUpdateInfo? _updateInfo;
    private bool _downloadedUpdate;

    public UpdateWindow()
    {
        InitializeComponent();
        CurrentVersionText.Text = _updateService.GetCurrentVersion();
        LatestVersionText.Text = "-";
    }

    private async void CheckButton_Click(object sender, RoutedEventArgs e)
    {
        SetBusy(true, "更新を確認しています...");
        ClearError();
        UpdateButton.IsEnabled = false;
        DownloadProgress.Visibility = Visibility.Collapsed;
        DownloadProgress.Value = 0;

        try
        {
            _updateInfo = await _updateService.CheckForUpdatesAsync();
            LatestVersionText.Text = string.IsNullOrWhiteSpace(_updateInfo.LatestVersion) ? "-" : _updateInfo.LatestVersion;
            ReleaseNotesText.Text = _updateInfo.ReleaseNotes;

            if (!_updateInfo.HasUpdate)
            {
                StatusText.Text = "現在最新版です。";
                UpdateButton.IsEnabled = false;
                return;
            }

            StatusText.Text = "新しいバージョンがあります。";
            UpdateButton.IsEnabled = _updateInfo.CanApplyUpdates;
        }
        catch (Exception ex)
        {
            StatusText.Text = "更新確認に失敗しました。";
            ShowError("インターネット接続または GitHub Releases の公開状態を確認して、しばらくしてから再度お試しください。\n\n" + ex.Message);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void UpdateButton_Click(object sender, RoutedEventArgs e)
    {
        if (_updateInfo == null)
            return;

        if (!_downloadedUpdate)
        {
            await DownloadUpdateAsync();
            if (!_downloadedUpdate)
                return;
        }

        var result = MessageBox.Show(
            "更新を適用するにはアプリを再起動します。\n今すぐ再起動しますか？",
            "TimeKeeper 更新",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
        {
            StatusText.Text = "更新ファイルのダウンロードは完了しています。更新する場合は再度「更新する」を押してください。";
            return;
        }

        try
        {
            _updateService.ApplyUpdatesAndRestart();
        }
        catch (Exception ex)
        {
            ShowError("更新の適用に失敗しました。\n\n" + ex.Message);
        }
    }

    private async Task DownloadUpdateAsync()
    {
        SetBusy(true, "更新ファイルをダウンロードしています...");
        ClearError();
        DownloadProgress.Visibility = Visibility.Visible;
        DownloadProgress.Value = 0;

        try
        {
            var progress = new Progress<int>(value =>
            {
                DownloadProgress.Value = Math.Max(0, Math.Min(100, value));
            });

            await _updateService.DownloadUpdatesAsync(progress);
            _downloadedUpdate = true;
            StatusText.Text = "更新ファイルのダウンロードが完了しました。";
        }
        catch (Exception ex)
        {
            StatusText.Text = "更新ダウンロードに失敗しました。";
            ShowError("インターネット接続を確認して、しばらくしてから再度お試しください。\n\n" + ex.Message);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool isBusy, string? status = null)
    {
        CheckButton.IsEnabled = !isBusy;
        UpdateButton.IsEnabled = !isBusy && _updateInfo?.HasUpdate == true && _updateInfo.CanApplyUpdates;
        if (!string.IsNullOrWhiteSpace(status))
            StatusText.Text = status;
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }

    private void ClearError()
    {
        ErrorText.Text = "";
        ErrorText.Visibility = Visibility.Collapsed;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
