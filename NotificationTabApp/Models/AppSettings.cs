using System.Collections.Generic;
using System;

namespace NotificationTabApp.Models
{
    public class MainWindowSettings
    {
        public double X { get; set; } = -1;
        public double Y { get; set; } = -1;
        public double Width { get; set; } = 260;
        public double Height { get; set; } = 48;
        public bool Pinned { get; set; } = true;
        public double OpacityNormal { get; set; } = 0.5;
        public double OpacityHover { get; set; } = 0.8;
    }

    public class AppSettings
    {
        public MainWindowSettings MainWindow { get; set; } = new();
        public List<NotificationGroup> NotificationGroups { get; set; } = new();
        public List<NotificationItem> Notifications { get; set; } = new();
        public List<NotificationItem> OneTimeNotifications { get; set; } = new();
        public List<SkippedNotificationOccurrence> SkippedOccurrences { get; set; } = new();
    }

    public class NotificationGroup
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
        public string CreatedAt { get; set; } = DateTime.Now.ToString("o");
        public string UpdatedAt { get; set; } = DateTime.Now.ToString("o");
    }

    public class SkippedNotificationOccurrence
    {
        public string NotificationId { get; set; } = string.Empty;
        public string TriggerKey { get; set; } = string.Empty;
    }
}
