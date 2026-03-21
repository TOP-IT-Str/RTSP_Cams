using LibVLCSharp.Shared;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace RTSP_Cams
{
    public partial class FullscreenWindow : Window
    {
        private readonly LibVLC _libVLC;

        private readonly MediaPlayer _subMediaPlayer;
        private readonly MediaPlayer _mainMediaPlayer;

        private Media? _subMedia;
        private Media? _mainMedia;

        private bool _isClosing;

        private readonly string _subUrl;
        private readonly string _mainUrl;
        private bool _mainStreamActivated;

        public FullscreenWindow(LibVLC libVLC, string title, string subUrl, string mainUrl)
        {
            InitializeComponent();

            _libVLC = libVLC;
            _subUrl = subUrl;
            _mainUrl = mainUrl;

            TitleText.Text = title;

            _subMediaPlayer = new MediaPlayer(_libVLC)
            {
                EnableHardwareDecoding = true,
                Mute = true
            };

            _mainMediaPlayer = new MediaPlayer(_libVLC)
            {
                EnableHardwareDecoding = true,
                Mute = false
            };

            subVideoView.MediaPlayer = _subMediaPlayer;
            mainVideoView.MediaPlayer = _mainMediaPlayer;

            Loaded += FullscreenWindow_Loaded;
            Closing += FullscreenWindow_Closing;
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
            _subMedia.AddOption(":rtsp-tcp");
            _subMedia.AddOption(":network-caching=150");
            _subMedia.AddOption(":live-caching=150");
            _subMedia.AddOption(":no-audio");

            try
            {
                _subMediaPlayer.Mute = true;
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
            _mainMedia.AddOption(":rtsp-tcp");
            _mainMedia.AddOption(":network-caching=300");
            _mainMedia.AddOption(":live-caching=300");

            try
            {
                _mainMediaPlayer.Mute = false;
                _mainMediaPlayer.Volume = 100;
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
    }
}