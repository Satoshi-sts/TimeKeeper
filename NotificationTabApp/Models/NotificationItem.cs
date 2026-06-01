using System;
using System.Collections.Generic;

namespace NotificationTabApp.Models
{
    public class NotificationTimeRange
    {
        public string StartTime { get; set; } = string.Empty;  // HH:mm
        public string EndTime { get; set; } = string.Empty;    // HH:mm or 24:00
    }

    public class NotificationItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string GroupId { get; set; } = string.Empty;
        public string TimeMode { get; set; } = "real";       // real / eorzea
        public string StartTime { get; set; } = string.Empty;  // HH:mm
        public string EndTime { get; set; } = string.Empty;    // HH:mm or 24:00
        public List<NotificationTimeRange> TimeRanges { get; set; } = new();
        public string Message { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
        public bool IsOneTime { get; set; } = false;
        public bool HasFired { get; set; } = false;
        public string CreatedAt { get; set; } = DateTime.Now.ToString("o");
        public string UpdatedAt { get; set; } = DateTime.Now.ToString("o");
    }
}
