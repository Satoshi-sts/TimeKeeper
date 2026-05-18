using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using NotificationTabApp.Models;

namespace NotificationTabApp.Services
{
    public class SettingsService
    {
        private static readonly string FolderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NotificationTabApp");

        private static readonly string FilePath = Path.Combine(FolderPath, "settings.json");
        private static readonly string BackupPath = Path.Combine(FolderPath, "settings.json.bak");

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never
        };

        public AppSettings Load()
        {
            if (!File.Exists(FilePath))
                return new AppSettings();

            try
            {
                var json = File.ReadAllText(FilePath);
                return Normalize(JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings(), removeFiredOneTimeNotifications: true);
            }
            catch
            {
                try { File.Copy(FilePath, BackupPath, overwrite: true); } catch { }

                MessageBox.Show(
                    "設定ファイルの読み込みに失敗しました。初期設定で起動します。\nバックアップ: settings.json.bak",
                    "設定読み込みエラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return new AppSettings();
            }
        }

        public void Save(AppSettings settings)
        {
            Normalize(settings, removeFiredOneTimeNotifications: false);
            Directory.CreateDirectory(FolderPath);
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(FilePath, json);
        }

        private static AppSettings Normalize(AppSettings settings, bool removeFiredOneTimeNotifications)
        {
            settings.NotificationGroups ??= new();
            settings.Notifications ??= new();
            settings.OneTimeNotifications ??= new();
            settings.SkippedOccurrences ??= new();

            foreach (var group in settings.NotificationGroups)
            {
                group.Name = group.Name?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(group.Id))
                    group.Id = Guid.NewGuid().ToString();
            }

            foreach (var item in settings.Notifications)
                NormalizeNotificationItem(item, isOneTime: false);

            foreach (var item in settings.OneTimeNotifications)
                NormalizeNotificationItem(item, isOneTime: true);

            if (removeFiredOneTimeNotifications)
                settings.OneTimeNotifications.RemoveAll(i => i.HasFired);
            settings.SkippedOccurrences.RemoveAll(s =>
                string.IsNullOrWhiteSpace(s.NotificationId) || string.IsNullOrWhiteSpace(s.TriggerKey));

            return settings;
        }

        private static void NormalizeNotificationItem(NotificationItem item, bool isOneTime)
        {
            if (!string.Equals(item.TimeMode, "eorzea", StringComparison.OrdinalIgnoreCase))
                item.TimeMode = "real";
            else
                item.TimeMode = "eorzea";

            item.GroupId ??= string.Empty;
            item.IsOneTime = isOneTime;
        }
    }
}
