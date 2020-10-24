using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ProtectMyProstate
{
    class Settings
    {
        public float WorkDuration { get; set; }
        public float RestDuration { get; set; }
        public float ContinueDuration { get; set; }
    }

    enum ProtectState
    {
        Work,
        Rest
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Settings ProjectSettings { get; set; }
        private Timer TaskTimer { get; set; }
        private ProtectState ProtectState { get; set; }
        private DateTime CurrentEndTime { get; set; }
        private bool Stop { get; set; }

        private static double Hour { get { return 3600.0; } }
        private static double Minute { get { return 60.0; } }
        private RestWindow RestWindow { get; set; }

        public MainWindow()
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            InitializeComponent();
            Hide();
            Stop = false;

            // 读取配置文件
            if (File.Exists("config.json"))
            {
                var file = File.ReadAllText("config.json");
                ProjectSettings = JsonConvert.DeserializeObject<Settings>(file);
            }
            else
            {
                ProjectSettings = new Settings
                {
                    WorkDuration = 2,
                    RestDuration = 15,
                    ContinueDuration = 20
                };
                var str = JsonConvert.SerializeObject(ProjectSettings);
                File.WriteAllText("config.json", str);
            }

            // 初始化组件内的值
            var WorkDuration = (TextBox)FindName("WorkDuration");
            WorkDuration.Text = ProjectSettings.WorkDuration.ToString();
            var RestDuration = (TextBox)FindName("RestDuration");
            RestDuration.Text = ProjectSettings.RestDuration.ToString();
            var ContinueDuration = (TextBox)FindName("ContinueDuration");
            ContinueDuration.Text = ProjectSettings.ContinueDuration.ToString();

            StateChanged += WindowStateChanged;

            RestWindow = new RestWindow();
            RestWindow.ContinueEvent += delegate
            {
                Continue();
            };

            ProtectState = ProtectState.Work;
            CurrentEndTime = DateTime.Now.AddSeconds(ProjectSettings.WorkDuration * Hour);

            TaskTimer = new Timer
            {
                Interval = 1000
            };
            TaskTimer.Elapsed += OnUpdate;
            TaskTimer.Start();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            if (Visibility == Visibility.Visible)
            {
                e.Cancel = MessageBox.Show("确定关闭?", "关闭窗口", MessageBoxButton.YesNo) == MessageBoxResult.No;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Dispatcher.Invoke(new Action(
                delegate
                {
                    RestWindow.Close();
                }
            ));
        }

        // 窗口状态
        private void WindowStateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide();
            }
        }

        // 打开设置界面
        private void OpenSettings(object sender, RoutedEventArgs e)
        {
            Show();
            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }
            Visibility = Visibility.Visible;
        }

        // 保存配置并开始
        private void SaveSettingsAndStart(object sender, RoutedEventArgs e)
        {
            var WorkDuration = (TextBox)FindName("WorkDuration");
            ProjectSettings.WorkDuration = float.Parse(WorkDuration.Text);
            var RestDuration = (TextBox)FindName("RestDuration");
            ProjectSettings.RestDuration = float.Parse(RestDuration.Text);
            var ContinueDuration = (TextBox)FindName("ContinueDuration");
            ProjectSettings.ContinueDuration = float.Parse(ContinueDuration.Text);
            var str = JsonConvert.SerializeObject(ProjectSettings);
            File.WriteAllText("config.json", str);
            WindowState = WindowState.Minimized;
            Restart();
        }

        // 限制输入的数据
        private void RequireNumberInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = Regex.IsMatch(e.Text, @"[^0-9^\.]+");
        }

        // 重新开始
        private void Restart()
        {
            Stop = false;
            ProtectState = ProtectState.Work;
            CurrentEndTime = DateTime.Now.AddSeconds(ProjectSettings.WorkDuration * Hour);
            Dispatcher.Invoke(new Action(
                delegate
                {
                    RestWindow.Hide();
                }
            ));
        }

        // 再敲一会
        private void Continue()
        {
            Stop = false;
            ProtectState = ProtectState.Work;
            CurrentEndTime = DateTime.Now.AddSeconds(ProjectSettings.ContinueDuration * Minute);
            Dispatcher.Invoke(new Action(
                delegate
                {
                    RestWindow.Hide();
                }
            ));
        }

        private void OnUpdate(object sender, ElapsedEventArgs e)
        {
            if (Stop)
            {
                return;
            }
            var now = DateTime.Now;
            var remaingTime = (CurrentEndTime - DateTime.Now).TotalSeconds;
            if (remaingTime <= 0)
            {
                if (ProtectState == ProtectState.Work)
                {
                    CurrentEndTime = DateTime.Now.AddSeconds(ProjectSettings.RestDuration * Minute);
                    ProtectState = ProtectState.Rest;
                    Dispatcher.Invoke(new Action(
                        delegate
                        {
                            RestWindow.Show();
                            RestWindow.SetRestTime((float)(CurrentEndTime - DateTime.Now).TotalSeconds);
                        }
                    ));
                }
                else if (ProtectState == ProtectState.Rest)
                {
                    CurrentEndTime = DateTime.Now.AddSeconds(ProjectSettings.WorkDuration * Hour);
                    ProtectState = ProtectState.Work;
                    Dispatcher.Invoke(new Action(
                        delegate
                        {
                            RestWindow.Hide();
                        }
                    ));
                }
            }

            if (ProtectState == ProtectState.Work)
            {
                var hour = (int)(remaingTime / 3600);
                var minute = (int)((remaingTime - hour * 3600) / 60);
                var secs = (int)(remaingTime - hour * 3600 - minute * 60);
                Dispatcher.Invoke(new Action(
                    delegate
                    {
                        RemaingWorkTime.Header = String.Format("{0:D2}:{1:D2}:{2:D2}", hour, minute, secs);
                    }
                ));
            }
            else
            {
                Dispatcher.Invoke(new Action(
                    delegate
                    {
                        RemaingWorkTime.Header = String.Format("{0:D2}:{1:D2}:{2:D2}", 0, 0, 0);
                    }
                ));
            }

            if (RestWindow.Visibility == Visibility.Visible && remaingTime >= 0)
            {
                Dispatcher.Invoke(new Action(
                    delegate
                    {
                        RestWindow.SetRestTime((float)(remaingTime));
                    }
                ));
            }
        }

        private void MenuItem_Start(object sender, RoutedEventArgs e)
        {
            Restart();
        }

        private void MenuItem_Stop(object sender, RoutedEventArgs e)
        {
            Stop = true;
            Dispatcher.Invoke(new Action(
                delegate
                {
                    RestWindow.Hide();
                }
            ));
        }

        private void MenuItem_Exit(object sender, RoutedEventArgs e)
        {
            Stop = true;
            Dispatcher.Invoke(new Action(
                delegate
                {
                    RestWindow.Close();
                    Close();
                }
            ));
        }
    }
}
