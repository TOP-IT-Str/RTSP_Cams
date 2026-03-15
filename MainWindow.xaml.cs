using LibVLCSharp.Shared;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RTSP_Cams2
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private const string SettingsFileName = "settings.json";

        private readonly LibVLC _libVLC;
        private int _gridColumns = 2;
        private bool _isShuttingDown;

        public ObservableCollection<CameraViewModel> Cameras { get; } = new();
        public AppSettings Settings { get; set; } = new();

        public int GridColumns
        {
            get => _gridColumns;
            set
            {
                _gridColumns = value;
                OnPropertyChanged();
            }
        }

        public MainWindow()
        {
            Core.Initialize();

            InitializeComponent();

            LoadSettings();
            DataContext = this;
            PasswordInput.Password = Settings.Password ?? string.Empty;

            _libVLC = new LibVLC(
                "--rtsp-tcp",
                "--network-caching=300",
                "--live-caching=300",
                "--drop-late-frames",
                "--skip-frames"
            );

            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
            KeyDown += MainWindow_KeyDown;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(Settings.IpAddress) &&
                !string.IsNullOrWhiteSpace(Settings.Username) &&
                Settings.CameraCount > 0)
            {
                StartStreams();
            }
            if (Settings.IsFullScreen)
            {
                EnableFullscreen();
            }
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
            Title = $"{Title} v{version}";
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F11)
                ToggleFullscreen();
        }

        private void PasswordInput_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            Settings.Password = PasswordInput.Password;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
            MessageBox.Show("Настройки сохранены.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isShuttingDown)
                return;

            SaveSettings();
            StartStreams();
        }

        private void StartStreams()
        {
            if (_isShuttingDown)
                return;

            StopAndClearStreams();

            int count = Settings.CameraCount;
            if (count < 1)
                return;

            for (int i = 1; i <= count; i++)
            {
                string title = $"Камера {i}";
                string url = BuildDahuaRtspUrl(Settings, i, mainStream: false);

                var vm = new CameraViewModel(_libVLC, title, url, i);
                Cameras.Add(vm);
            }

            UpdateGridColumns();

            foreach (var camera in Cameras)
                camera.Start();
        }

        private static string BuildDahuaRtspUrl(AppSettings settings, int channel, bool mainStream)
        {
            string login = Uri.EscapeDataString(settings.Username ?? string.Empty);
            string password = Uri.EscapeDataString(settings.Password ?? string.Empty);
            string ip = settings.IpAddress ?? string.Empty;
            int port = settings.RtspPort <= 0 ? 554 : settings.RtspPort;
            int subtype = mainStream ? 0 : 1;

            return $"rtsp://{login}:{password}@{ip}:{port}/cam/realmonitor?channel={channel}&subtype={subtype}";
        }

        private void FullscreenButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isShuttingDown)
                return;

            if (sender is not FrameworkElement element)
                return;

            if (element.Tag is not CameraViewModel camera)
                return;

            string mainUrl = BuildDahuaRtspUrl(Settings, camera.Channel, mainStream: true);

            var fullscreenWindow = new FullscreenWindow(_libVLC, camera.Title, mainUrl)
            {
                Owner = this
            };

            fullscreenWindow.Show();
        }

        private void StopAndClearStreams()
        {
            var snapshot = new List<CameraViewModel>(Cameras);

            Cameras.Clear();

            foreach (var camera in snapshot)
            {
                try
                {
                    camera.SafeShutdown();
                }
                catch
                {
                }
            }
        }

        private void UpdateGridColumns()
        {
            int count = Cameras.Count;

            if (count <= 1) GridColumns = 1;
            else if (count <= 4) GridColumns = 2;
            else if (count <= 9) GridColumns = 3;
            else if (count <= 16) GridColumns = 4;
            else GridColumns = 5;
        }

        private void LoadSettings()
        {
            try
            {
                if (!File.Exists(SettingsFileName))
                {
                    Settings = new AppSettings
                    {
                        IpAddress = "192.168.1.108",
                        Username = "admin",
                        Password = "",
                        CameraCount = 4,
                        RtspPort = 554,
                        IsFullScreen = false,
                    };
                    return;
                }

                string json = File.ReadAllText(SettingsFileName);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json);

                Settings = loaded ?? new AppSettings();
                if (Settings.RtspPort <= 0)
                    Settings.RtspPort = 554;
            }
            catch
            {
                Settings = new AppSettings
                {
                    IpAddress = "192.168.1.108",
                    Username = "admin",
                    Password = "",
                    CameraCount = 4,
                    RtspPort = 554,
                    IsFullScreen = false,
                };
            }
        }

        private void SaveSettings()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string json = JsonSerializer.Serialize(Settings, options);
            File.WriteAllText(SettingsFileName, json);
        }

        private async void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            if (_isShuttingDown)
                return;

            _isShuttingDown = true;
            e.Cancel = true;
            IsEnabled = false;

            try
            {
                SaveSettings();
            }
            catch
            {
            }

            bool closedGracefully = await ShutdownEverythingAsync(TimeSpan.FromSeconds(4));

            if (!closedGracefully)
            {
                ForceTerminateApplication();
                return;
            }

            Closing -= MainWindow_Closing;
            Close();
        }

        private async Task<bool> ShutdownEverythingAsync(TimeSpan timeout)
        {
            try
            {
                var ownedWindows = new List<Window>();
                foreach (Window owned in OwnedWindows)
                    ownedWindows.Add(owned);

                foreach (var owned in ownedWindows)
                {
                    try
                    {
                        owned.Close();
                    }
                    catch
                    {
                    }
                }

                var snapshot = new List<CameraViewModel>(Cameras);
                Cameras.Clear();

                await Dispatcher.InvokeAsync(() => { });

                var shutdownTask = Task.Run(() =>
                {
                    foreach (var camera in snapshot)
                    {
                        try
                        {
                            camera.SafeShutdown();
                        }
                        catch
                        {
                        }
                    }

                    try
                    {
                        _libVLC.Dispose();
                    }
                    catch
                    {
                    }
                });

                Task completedTask = await Task.WhenAny(shutdownTask, Task.Delay(timeout));
                return completedTask == shutdownTask;
            }
            catch
            {
                return false;
            }
        }

        private void ForceTerminateApplication()
        {
            try
            {
                Process.GetCurrentProcess().Kill(true);
            }
            catch
            {
                Environment.FailFast("RTSP_Cams2 forced termination.");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private void ShowSettings(object sender, MouseButtonEventArgs e)
        {
            if (SettingsBorder.Visibility == Visibility.Visible)
            {
                SettingsBorder.Visibility = Visibility.Collapsed;
                (ShowSettingsBorder.Child as TextBlock)?.Text = "▲";
            }
            else
            {
                SettingsBorder.Visibility = Visibility.Visible;
                (ShowSettingsBorder.Child as TextBlock)?.Text = "▼";
            }
        }

        private void ToggleFullscreen()
        {
            if (!Settings.IsFullScreen)
            {
                EnableFullscreen();
            }
            else
            {
                DisableFullscreen();
            }
        }

        private void EnableFullscreen()
        {
            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Maximized;
            ResizeMode = ResizeMode.NoResize;
            Topmost = true;

            Settings.IsFullScreen = true;
        }

        private void DisableFullscreen()
        {
            WindowStyle = WindowStyle.SingleBorderWindow;
            WindowState = WindowState.Normal;
            ResizeMode = ResizeMode.CanResize;
            Topmost = false;

            Settings.IsFullScreen = false;
        }

        private void FullScreenWindow_OnClick(object sender, RoutedEventArgs e)
        {
            ToggleFullscreen();
        }
    }
}