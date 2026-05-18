using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Input;
using System.Windows.Media;
using NotificationTabApp.Models;

namespace NotificationTabApp.Controls
{
    public partial class NotificationPopup : Window
    {
        public string NotificationId { get; }

        public event EventHandler<string>? UserClosed;

        public NotificationPopup(NotificationItem item)
        {
            InitializeComponent();
            NotificationId = item.Id;
            TitleText.Text = item.Title;
            TimeText.Text = $"{(item.TimeMode == "eorzea" ? "ET" : "現実時間")} {item.StartTime} - {item.EndTime}";
            MessageText.Text = item.Message;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var handle = new WindowInteropHelper(this).Handle;
            var exStyle = NativeMethods.GetWindowLong(handle, NativeMethods.GwlExStyle);
            NativeMethods.SetWindowLong(handle, NativeMethods.GwlExStyle, exStyle | NativeMethods.WsExNoActivate);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            UserClosed?.Invoke(this, NotificationId);
            Close();
        }

        private void PopupBorder_MouseEnter(object sender, MouseEventArgs e)
        {
            PopupBorder.Background = new SolidColorBrush(Color.FromArgb(0xD0, 0x22, 0x22, 0x22));
        }

        private void PopupBorder_MouseLeave(object sender, MouseEventArgs e)
        {
            PopupBorder.Background = new SolidColorBrush(Color.FromArgb(0xA8, 0x22, 0x22, 0x22));
        }

        private static class NativeMethods
        {
            public const int GwlExStyle = -20;
            public const int WsExNoActivate = 0x08000000;

            [System.Runtime.InteropServices.DllImport("user32.dll")]
            public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

            [System.Runtime.InteropServices.DllImport("user32.dll")]
            public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        }
    }
}
