using LibVLCSharp.Shared;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace RTSP_Cams2
{
    public partial class FullscreenWindow : Window
    {
        private readonly LibVLC _libVLC;
        private readonly MediaPlayer _mediaPlayer;
        private Media? _media;
        private bool _isClosing;
        private string? _startupUrl;

        public FullscreenWindow(LibVLC libVLC, string title, string url)
        {
            InitializeComponent();

            _libVLC = libVLC;
            _startupUrl = url;
            TitleText.Text = title;

            _mediaPlayer = new MediaPlayer(_libVLC)
            {
                EnableHardwareDecoding = true,
                Mute = false
            };

            videoView.MediaPlayer = _mediaPlayer;

            Loaded += FullscreenWindow_Loaded;
            Closing += FullscreenWindow_Closing;
        }

        private void FullscreenWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(_startupUrl))
                Start(_startupUrl);
        }

        private void Start(string url)
        {
            if (_isClosing)
                return;

            try
            {
                _media?.Dispose();
            }
            catch
            {
            }

            _media = new Media(_libVLC, url, FromType.FromLocation);
            _media.AddOption(":rtsp-tcp");
            _media.AddOption(":network-caching=150");
            _media.AddOption(":live-caching=150");

            try
            {
                _mediaPlayer.Mute = false;
                _mediaPlayer.Volume = 100;
                _mediaPlayer.Play(_media);
            }
            catch
            {
            }
        }

        private void FullscreenWindow_Closing(object? sender, CancelEventArgs e)
        {
            if (_isClosing)
                return;

            _isClosing = true;
            Cleanup();
        }

        private void Cleanup()
        {
            try
            {
                videoView.MediaPlayer = null;
            }
            catch
            {
            }

            try
            {
                _mediaPlayer.Mute = true;
            }
            catch
            {
            }

            try
            {
                _mediaPlayer.Media = null;
            }
            catch
            {
            }

            try
            {
                if (_mediaPlayer.IsPlaying)
                    _mediaPlayer.Stop();
            }
            catch
            {
            }

            try
            {
                _media?.Dispose();
                _media = null;
            }
            catch
            {
            }

            try
            {
                _mediaPlayer.Dispose();
            }
            catch
            {
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
                Close();
        }
    }
}