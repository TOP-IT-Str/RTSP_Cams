using LibVLCSharp.Shared;
using System;
using System.Windows;
using System.Windows.Input;

namespace RTSP_Cams2
{
    public partial class FullscreenWindow : Window
    {
        private readonly LibVLC _libVLC;
        private readonly MediaPlayer _mediaPlayer;
        private Media? _media;

        public FullscreenWindow(LibVLC libVLC, string title, string url)
        {
            InitializeComponent();

            _libVLC = libVLC;
            TitleText.Text = title;

            _mediaPlayer = new MediaPlayer(_libVLC)
            {
                EnableHardwareDecoding = true,
                Mute = false
            };

            videoView.MediaPlayer = _mediaPlayer;

            Loaded += (_, _) => Start(url);
            Closed += (_, _) => Cleanup();
        }

        private void Start(string url)
        {
            _media?.Dispose();

            _media = new Media(_libVLC, url, FromType.FromLocation);
            _media.AddOption(":rtsp-tcp");
            _media.AddOption(":network-caching=150");
            _media.AddOption(":live-caching=150");

            _mediaPlayer.Mute = false;
            _mediaPlayer.Volume = 100;
            _mediaPlayer.Play(_media);
        }

        private void Cleanup()
        {
            try
            {
                if (_mediaPlayer.IsPlaying)
                    _mediaPlayer.Stop();
            }
            catch
            {
            }

            _media?.Dispose();
            _mediaPlayer.Dispose();
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