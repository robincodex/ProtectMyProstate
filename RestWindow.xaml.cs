using System;
using System.Windows;

namespace ProtectMyProstate
{
    /// <summary>
    /// RestWindow.xaml 的交互逻辑
    /// </summary>
    public partial class RestWindow : Window
    {
        public event Action ContinueEvent;
        public RestWindow()
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            InitializeComponent();
            Topmost = true;
        }

        public void SetRestTime(float seconds)
        {
            var minute = (int)(seconds / 60);
            var secs = (int)(seconds - minute * 60);
            RestTime.Text = String.Format("休息倒计时 {0:D2}:{1:D2}", minute, secs);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ContinueEvent?.Invoke();
        }
    }
}
