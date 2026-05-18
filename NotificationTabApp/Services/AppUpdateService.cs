using System.Reflection;
using Velopack;
using Velopack.Sources;

namespace NotificationTabApp.Services;

public static class UpdateConfig
{
    public const string DistributionRepositoryOwner = "Satoshi-sts";
    public const string DistributionRepositoryName = "TimeKeeper-Releases";
    public const string DistributionRepositoryUrl = "https://github.com/Satoshi-sts/TimeKeeper-Releases";
    public const bool IncludePrereleases = false;
}

public sealed class AppUpdateInfo
{
    public bool HasUpdate { get; init; }
    public string CurrentVersion { get; init; } = "";
    public string LatestVersion { get; init; } = "";
    public string ReleaseNotes { get; init; } = "";
    public bool CanApplyUpdates { get; init; }
}

public class AppUpdateService
{
    private readonly UpdateManager _updateManager;
    private Velopack.UpdateInfo? _pendingUpdate;

    public AppUpdateService()
        : this(CreateUpdateManager())
    {
    }

    internal AppUpdateService(UpdateManager updateManager)
    {
        _updateManager = updateManager;
    }

    public string UpdateSourceDescription => UpdateConfig.DistributionRepositoryUrl;

    public string GetCurrentVersion()
    {
        return _updateManager.CurrentVersion?.ToString()
            ?? GetAssemblyVersion();
    }

    public async Task<AppUpdateInfo> CheckForUpdatesAsync()
    {
        if (!_updateManager.IsInstalled)
        {
            return new AppUpdateInfo
            {
                HasUpdate = false,
                CurrentVersion = GetCurrentVersion(),
                LatestVersion = "-",
                ReleaseNotes = "Velopack でインストールされたアプリではないため、この実行環境では更新確認できません。Setup.exe からインストールしたアプリで確認してください。",
                CanApplyUpdates = false
            };
        }

        _pendingUpdate = await _updateManager.CheckForUpdatesAsync();
        if (_pendingUpdate == null)
        {
            return new AppUpdateInfo
            {
                HasUpdate = false,
                CurrentVersion = GetCurrentVersion(),
                LatestVersion = "-",
                ReleaseNotes = "現在最新版です。",
                CanApplyUpdates = true
            };
        }

        var target = _pendingUpdate.TargetFullRelease;
        return new AppUpdateInfo
        {
            HasUpdate = true,
            CurrentVersion = GetCurrentVersion(),
            LatestVersion = target.Version.ToString(),
            ReleaseNotes = string.IsNullOrWhiteSpace(target.NotesMarkdown)
                ? "更新内容は登録されていません。"
                : target.NotesMarkdown,
            CanApplyUpdates = true
        };
    }

    public async Task DownloadUpdatesAsync(IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        if (_pendingUpdate == null)
            throw new InvalidOperationException("先に更新確認を実行してください。");

        Action<int>? progressCallback = progress == null ? null : value => progress.Report(value);
        await _updateManager.DownloadUpdatesAsync(_pendingUpdate, progressCallback, cancellationToken);
    }

    public void ApplyUpdatesAndRestart()
    {
        if (_pendingUpdate == null)
            throw new InvalidOperationException("適用可能な更新がありません。");

        _updateManager.ApplyUpdatesAndRestart(_pendingUpdate.TargetFullRelease);
    }

    private static UpdateManager CreateUpdateManager()
    {
        var source = new GithubSource(
            UpdateConfig.DistributionRepositoryUrl,
            accessToken: "",
            prerelease: UpdateConfig.IncludePrereleases);

        return new UpdateManager(source);
    }

    private static string GetAssemblyVersion()
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var infoVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(infoVersion))
            return infoVersion.Split('+')[0];

        return assembly.GetName().Version?.ToString() ?? "0.0.0";
    }
}
