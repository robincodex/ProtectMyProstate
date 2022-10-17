using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using System.Linq;

namespace ProtectMyProstate
{
    class Settings
    {
        public float SitDuration { get; set; }
        public float StandDuration { get; set; }
    }

    enum ProtectState
    {
        Sit,
        Stand
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

        private static double Minute { get { return 60.0; } }
        private RestWindow RestWindow { get; set; }
        private bool IsOpenRestWindow { get; set; }

        public MainWindow()
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            InitializeComponent();
            Hide();
            Stop = false;
            IsOpenRestWindow = false;

            // 读取配置文件
            if (File.Exists("config.json"))
            {
                var file = File.ReadAllText("config.json");
                ProjectSettings = JsonConvert.DeserializeObject<Settings>(file);
            }
            else
            {
            #if DEBUG
                ProjectSettings = new Settings
                {
                    SitDuration = 0.15f,
                    StandDuration = 0.15f,
                };
            #else
                ProjectSettings = new Settings
                {
                    SitDuration = 45,
                    StandDuration = 15,
                };
            #endif
                var str = JsonConvert.SerializeObject(ProjectSettings);
                File.WriteAllText("config.json", str);
            }

            // 初始化组件内的值
            var SitDuration = (TextBox)FindName("SitDuration");
            SitDuration.Text = ProjectSettings.SitDuration.ToString();
            var StandDuration = (TextBox)FindName("StandDuration");
            StandDuration.Text = ProjectSettings.StandDuration.ToString();

            StateChanged += WindowStateChanged;

            RestWindow = new RestWindow();
            RestWindow.ContinueEvent += delegate
            {
                this.OnContinue();
            };

            ProtectState = ProtectState.Sit;
            CurrentEndTime = DateTime.Now.AddSeconds(ProjectSettings.SitDuration * Minute);

            TaskTimer = new Timer
            {
                Interval = 1000
            };
            TaskTimer.Elapsed += OnUpdate;
            TaskTimer.Start();

            SystemEvents.PowerModeChanged += OnPowerModeChanged;
        }
        private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if(e.Mode == PowerModes.Resume)
            {
                Restart();
            }
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
            var SitDuration = (TextBox)FindName("SitDuration");
            ProjectSettings.SitDuration = float.Parse(SitDuration.Text);
            var StandDuration = (TextBox)FindName("StandDuration");
            ProjectSettings.StandDuration = float.Parse(StandDuration.Text);
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
            ProtectState = ProtectState.Sit;
            CurrentEndTime = DateTime.Now.AddSeconds(ProjectSettings.SitDuration * Minute);
            Dispatcher.Invoke(new Action(
                delegate
                {
                    RestWindow.Hide();
                }
            ));
        }

        private void RestartToStand()
        {
            Stop = false;
            ProtectState = ProtectState.Stand;
            CurrentEndTime = DateTime.Now.AddSeconds(ProjectSettings.StandDuration * Minute);
            Dispatcher.Invoke(new Action(
                delegate
                {
                    RestWindow.Hide();
                }
            ));
        }

        private void OnContinue()
        {
            if (ProtectState == ProtectState.Sit)
            {
                CurrentEndTime = DateTime.Now.AddSeconds(ProjectSettings.StandDuration * Minute);
                ProtectState = ProtectState.Stand;
            }
            else if (ProtectState == ProtectState.Stand)
            {
                CurrentEndTime = DateTime.Now.AddSeconds(ProjectSettings.SitDuration * Minute);
                ProtectState = ProtectState.Sit;
            }
            RestWindow.Hide();
            IsOpenRestWindow = false;
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
                if (IsOpenRestWindow)
                {
                    return;
                }
                if (ProtectState == ProtectState.Sit)
                {
                    Dispatcher.Invoke(new Action(
                        delegate
                        {
                            RestWindow.Show();
                            RestWindow.SetContent("/Resource/站立.png", "站起来了!");
                        }
                    ));
                    IsOpenRestWindow = true;
                } else
                {
                    Dispatcher.Invoke(new Action(
                        delegate
                        {
                            RestWindow.Show();
                            RestWindow.SetContent("/Resource/半蹲.png", "可以坐下了~");
                        }
                    ));
                    IsOpenRestWindow = true;
                }
            }

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

        private void MenuItem_Start(object sender, RoutedEventArgs e)
        {
            Restart();
        }

        private void MenuItem_StartStand(object sender, RoutedEventArgs e)
        {
            RestartToStand();
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
