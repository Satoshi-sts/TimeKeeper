using System.Windows;
using NotificationTabApp.Models;
using NotificationTabApp.Services;

namespace NotificationTabApp;

public partial class App : Application
{
    public static SettingsService SettingsService { get; } = new SettingsService();
    public static AppSettings Settings { get; private set; } = new AppSettings();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Settings = SettingsService.Load();
    }
}

