using LibVLCSharp.Shared;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Windows.Media;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;

namespace RtspGridDemo
{
    public sealed class CameraViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly LibVLC _libVLC;
        private Media? _media;
        private string _status = "Ожидание";

        public string Title { get; }
        public string Url { get; }
        public MediaPlayer MediaPlayer { get; }

        public string Status
        {
            get => _status;
            private set
            {
                _status = value;
                OnPropertyChanged();
            }
        }

        public CameraViewModel(LibVLC libVLC, string title, string url)
        {
            _libVLC = libVLC;
            Title = title;
            Url = url;

            MediaPlayer = new MediaPlayer(_libVLC)
            {
                EnableHardwareDecoding = true
            };

            MediaPlayer.Opening += (_, _) => Status = "Подключение";
            MediaPlayer.Buffering += (_, e) => Status = $"Буферизация {e.Cache:0}%";
            MediaPlayer.Playing += (_, _) => Status = "В эфире";
            MediaPlayer.EncounteredError += (_, _) => Status = "Ошибка";
            MediaPlayer.EndReached += (_, _) => Status = "Поток завершён";
            MediaPlayer.Stopped += (_, _) => Status = "Остановлено";
        }

        public void Start()
        {
            _media?.Dispose();
            _media = new Media(_libVLC, new Uri(Url));
            _media.AddOption(":rtsp-tcp");
            _media.AddOption(":network-caching=300");
            _media.AddOption(":live-caching=300");
            _media.AddOption(":no-audio");

            MediaPlayer.Play(_media);
        }

        public void Stop()
        {
            if (MediaPlayer.IsPlaying)
                MediaPlayer.Stop();
        }

        public void Dispose()
        {
            try
            {
                Stop();
            }
            catch
            {
            }

            _media?.Dispose();
            MediaPlayer.Dispose();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}