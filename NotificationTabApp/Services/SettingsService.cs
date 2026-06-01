using System;
using System.IO;
using System.Linq;
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
            item.TimeRanges ??= new();

            item.TimeRanges = item.TimeRanges
                .Where(r => IsValidStartTime(r.StartTime) && IsValidEndTime(r.EndTime) && r.StartTime != r.EndTime)
                .Select(r => new NotificationTimeRange
                {
                    StartTime = r.StartTime,
                    EndTime = r.EndTime
                })
                .ToList();

            if (item.TimeRanges.Count == 0 &&
                IsValidStartTime(item.StartTime) &&
                IsValidEndTime(item.EndTime) &&
                item.StartTime != item.EndTime)
            {
                item.TimeRanges.Add(new NotificationTimeRange
                {
                    StartTime = item.StartTime,
                    EndTime = item.EndTime
                });
            }

            if (item.TimeRanges.Count > 0)
            {
                item.StartTime = item.TimeRanges[0].StartTime;
                item.EndTime = item.TimeRanges[0].EndTime;
            }
        }

        private static bool IsValidStartTime(string? time)
            => TryParseTime(time, allow24Hour: false, out _);

        private static bool IsValidEndTime(string? time)
            => TryParseTime(time, allow24Hour: true, out _);

        private static bool TryParseTime(string? time, bool allow24Hour, out int minutesOfDay)
        {
            minutesOfDay = 0;
            if (string.IsNullOrEmpty(time)) return false;

            var parts = time.Split(':');
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
    }
}
