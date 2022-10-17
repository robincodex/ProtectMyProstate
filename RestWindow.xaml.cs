using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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

        public void SetContent(string imgSrc, string text)
        {
            var Icon = (Image)FindName("Icon");
            Icon.Source = new BitmapImage(new Uri(imgSrc, UriKind.Relative));
            var Title = (TextBlock)FindName("Title");
            Title.Text = text;
            count = 0;
        }

        private int count = 0;

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if(count <= 2)
            {
                count++;
                var btn = (Button)FindName("ContinueButton");
                btn.Content = count;
                return;
            }
            ContinueEvent?.Invoke();
        }
    }
}
