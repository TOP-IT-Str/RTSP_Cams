using LibVLCSharp.Shared;
using RTSP_Cams.Settings;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace RTSP_Cams
{
    public partial class FullscreenWindow : Window
    {
        private DispatcherTimer timer;

        private readonly LibVLC _libVLC;

        private readonly MediaPlayer _subMediaPlayer;
        private readonly MediaPlayer _mainMediaPlayer;

        private Media? _subMedia;
        private Media? _mainMedia;

        private bool _isClosing;

        private readonly string _subUrl;
        private readonly string _mainUrl;
        private bool _mainStreamActivated;

        AppSettings _settings;

        public FullscreenWindow(LibVLC libVLC, string title, string subUrl, string mainUrl, AppSettings settings)
        {
            InitializeComponent();

            _libVLC = libVLC;
            _subUrl = subUrl;
            _mainUrl = mainUrl;
            _settings = settings;

            TitleText.Text = title;

            _subMediaPlayer = new MediaPlayer(_libVLC)
            {
                EnableHardwareDecoding = true,
                Mute = !_settings.VlcSettings.SubFullscreen.Audio
            };

            _mainMediaPlayer = new MediaPlayer(_libVLC)
            {
                EnableHardwareDecoding = true,
                Mute = !_settings.VlcSettings.MainFullscreen.Audio
            };

            subVideoView.MediaPlayer = _subMediaPlayer;
            mainVideoView.MediaPlayer = _mainMediaPlayer;

            Loaded += FullscreenWindow_Loaded;
            Closing += FullscreenWindow_Closing;
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timer.Start();
            Timer_Tick(timer, null);
        }

        private void FullscreenWindow_Loaded(object sender, RoutedEventArgs e)
        {
            StartSubStream();
            StartMainStream();
            _ = WaitForFirstMainFrameAsync();
        }

        private void StartSubStream()
        {
            if (_isClosing)
                return;

            try
            {
                _subMedia?.Dispose();
            }
            catch
            {
            }

            _subMedia = new Media(_libVLC, _subUrl, FromType.FromLocation);
            _settings.VlcSettings.SubFullscreen.ApplyTo(_subMedia);

            try
            {
                _subMediaPlayer.Mute = !_settings.VlcSettings.SubFullscreen.Audio;
                _mainMediaPlayer.Volume = _settings.Volume;
                _subMediaPlayer.Play(_subMedia);
            }
            catch
            {
            }
        }

        private void StartMainStream()
        {
            if (_isClosing)
                return;

            try
            {
                _mainMedia?.Dispose();
            }
            catch
            {
            }

            _mainMedia = new Media(_libVLC, _mainUrl, FromType.FromLocation);
            _settings.VlcSettings.MainFullscreen.ApplyTo(_mainMedia);

            try
            {
                _mainMediaPlayer.Mute = !_settings.VlcSettings.MainFullscreen.Audio;
                _mainMediaPlayer.Volume = _settings.Volume;
                _mainMediaPlayer.Play(_mainMedia);
            }
            catch
            {
            }
        }

        private async Task WaitForFirstMainFrameAsync()
        {
            while (true)
            {
                if (_isClosing || _mainStreamActivated)
                    return;
                if (!_mainMediaPlayer.IsPlaying)
                {
                    await Task.Delay(100);
                    continue;
                }
                await Task.Delay(120);
                try
                {
                    if (_mainMediaPlayer.Fps > 0)
                    {
                        ActivateMainStream();
                        return;
                    }
                }
                catch
                {
                }
            }
        }

        private void ActivateMainStream()
        {
            if (_isClosing || _mainStreamActivated)
                return;

            _mainStreamActivated = true;

            Dispatcher.Invoke(() =>
            {
                if (_isClosing)
                    return;
                subVideoView.Visibility = Visibility.Collapsed;
            });

            try
            {
                if (_subMediaPlayer.IsPlaying)
                    _subMediaPlayer.Stop();
            }
            catch
            {
            }

            try
            {
                _subMedia?.Dispose();
                _subMedia = null;
            }
            catch
            {
            }
        }

        private void FullscreenWindow_Closing(object? sender, CancelEventArgs e)
        {
            Hide();
            if (_isClosing)
                return;

            _isClosing = true;
            Cleanup();
        }

        private void CleanupMain()
        {
            try
            {
                mainVideoView.MediaPlayer = null;
            }
            catch
            {
            }
            try
            {
                _mainMediaPlayer.Mute = true;
            }
            catch
            {
            }
            try
            {
                _mainMediaPlayer.Media = null;
            }
            catch
            {
            }
            try
            {
                if (_mainMediaPlayer.IsPlaying)
                    _mainMediaPlayer.Stop();
            }
            catch
            {
            }
            try
            {
                _mainMedia?.Dispose();
                _mainMedia = null;
            }
            catch
            {
            }
            try
            {
                _mainMediaPlayer.Dispose();
            }
            catch
            {
            }
        }

        private void CleanupSub()
        {
            try
            {
                subVideoView.MediaPlayer = null;
            }
            catch
            {
            }
            try
            {
                _subMediaPlayer.Media = null;
            }
            catch
            {
            }
            try
            {
                if (_subMediaPlayer.IsPlaying)
                    _subMediaPlayer.Stop();
            }
            catch
            {
            }
            try
            {
                _subMedia?.Dispose();
                _subMedia = null;
            }
            catch
            {
            }
            try
            {
                _subMediaPlayer.Dispose();
            }
            catch
            {
            }
        }

        private void Cleanup()
        {
            CleanupMain();
            CleanupSub();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }

        private void CloseBtn_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            Close();
        }

        private (uint width, uint height)? GetVideoResolution(MediaPlayer player)
        {
            var tracks = player.Media?.Tracks;
            if (tracks == null)
                return null;

            foreach (var track in tracks)
            {
                if (track.TrackType == TrackType.Video)
                {
                    var video = track.Data.Video;

                    return (video.Width, video.Height);
                }
            }

            return null;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_isClosing)
                return;
            MediaPlayer activePlayer = _mainStreamActivated ? _mainMediaPlayer : _subMediaPlayer;
            StreamTypeText.Text = _mainStreamActivated ? "Main stream" : "Sub stream (waiting for main stream...)";
            var res = GetVideoResolution(activePlayer);
            if (res != null)
            {
                var (w, h) = res.Value;
                StreamTypeText.Text += $" ({w}x{h})";
            }
            try
            {
                StreamTypeText.Text += $" - FPS: {activePlayer.Fps:0.}";
            }
            catch
            {
            }
        }
    }
}