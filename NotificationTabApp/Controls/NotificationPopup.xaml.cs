using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using NotificationTabApp.Models;

namespace NotificationTabApp.Controls
{
    public partial class NotificationPopup : Window
    {
        public string NotificationId { get; }

        public event EventHandler<string>? UserClosed;
        public event EventHandler<string>? OpenRequested;
        public event EventHandler<string>? MuteRequested;

        public NotificationPopup(NotificationItem item, NotificationTimeRange? timeRange = null)
        {
            InitializeComponent();
            NotificationId = item.Id;
            var displayRange = timeRange ?? item.TimeRanges.FirstOrDefault() ?? new NotificationTimeRange
            {
                StartTime = item.StartTime,
                EndTime = item.EndTime
            };
            TitleText.Text = item.Title;
            TimeText.Text = $"{(item.TimeMode == "eorzea" ? "ET" : "現実時間")} {displayRange.StartTime} - {displayRange.EndTime}";
            MessageText.Text = item.Message;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            UserClosed?.Invoke(this, NotificationId);
            Close();
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            OpenRequested?.Invoke(this, NotificationId);
        }

        private void MuteButton_Click(object sender, RoutedEventArgs e)
        {
            MuteRequested?.Invoke(this, NotificationId);
        }

        private void PopupBorder_MouseEnter(object sender, MouseEventArgs e)
        {
            PopupBorder.Background = new SolidColorBrush(Color.FromArgb(0xD0, 0x22, 0x22, 0x22));
        }

        private void PopupBorder_MouseLeave(object sender, MouseEventArgs e)
        {
            PopupBorder.Background = new SolidColorBrush(Color.FromArgb(0xA8, 0x22, 0x22, 0x22));
        }

    }
}
