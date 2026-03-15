using LibVLCSharp.Shared;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;

namespace RtspGridDemo
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private const string SettingsFileName = "settings.json";

        private readonly LibVLC _libVLC;
        private int _gridColumns = 2;

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
                "--no-audio",
                "--drop-late-frames",
                "--skip-frames"
            );

            Closing += MainWindow_Closing;
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
            SaveSettings();
            StartStreams();
        }

        private void StartStreams()
        {
            StopAndClearStreams();

            int count = Settings.CameraCount;
            if (count < 1)
                return;

            for (int i = 1; i <= count; i++)
            {
                string title = $"Камера {i}";
                string url = BuildDahuaRtspUrl(Settings, i);

                var vm = new CameraViewModel(_libVLC, title, url);
                Cameras.Add(vm);
            }

            UpdateGridColumns();

            foreach (var camera in Cameras)
                camera.Start();
        }

        private static string BuildDahuaRtspUrl(AppSettings settings, int channel)
        {
            string login = Uri.EscapeDataString(settings.Username ?? string.Empty);
            string password = Uri.EscapeDataString(settings.Password ?? string.Empty);
            string ip = settings.IpAddress ?? string.Empty;

            return $"rtsp://{login}:{password}@{ip}:554/cam/realmonitor?channel={channel}&subtype=1";
        }

        private void StopAndClearStreams()
        {
            foreach (var camera in Cameras)
                camera.Dispose();

            Cameras.Clear();
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
                        CameraCount = 4
                    };
                    return;
                }

                string json = File.ReadAllText(SettingsFileName);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json);

                Settings = loaded ?? new AppSettings();
            }
            catch
            {
                Settings = new AppSettings
                {
                    IpAddress = "192.168.1.108",
                    Username = "admin",
                    Password = "",
                    CameraCount = 4
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

        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            SaveSettings();
            StopAndClearStreams();
            _libVLC.Dispose();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}