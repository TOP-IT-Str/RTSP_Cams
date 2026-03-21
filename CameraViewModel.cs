using LibVLCSharp.Shared;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RTSP_Cams
{
    public sealed class CameraViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly LibVLC _libVLC;
        private Media? _media;
        private string _status = "Ожидание";
        private bool _isDisposed;

        public string Title { get; }
        public string Url { get; private set; }
        public int Channel { get; }
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

        public CameraViewModel(LibVLC libVLC, string title, string url, int channel)
        {
            _libVLC = libVLC;
            Title = title;
            Url = url;
            Channel = channel;

            MediaPlayer = new MediaPlayer(_libVLC)
            {
                EnableHardwareDecoding = true,
                Mute = true
            };

            MediaPlayer.Opening += MediaPlayer_Opening;
            MediaPlayer.Buffering += MediaPlayer_Buffering;
            MediaPlayer.Playing += MediaPlayer_Playing;
            MediaPlayer.EncounteredError += MediaPlayer_EncounteredError;
            MediaPlayer.EndReached += MediaPlayer_EndReached;
            MediaPlayer.Stopped += MediaPlayer_Stopped;
        }

        public void Start()
        {
            StartWithUrl(Url, muted: true);
        }

        public void StartWithUrl(string url, bool muted)
        {
            if (_isDisposed)
                return;

            Url = url;

            try
            {
                _media?.Dispose();
            }
            catch
            {
            }

            _media = new Media(_libVLC, url, FromType.FromLocation);
            _media.AddOption(":rtsp-tcp");
            _media.AddOption(":network-caching=300");
            _media.AddOption(":live-caching=300");

            if (muted)
                _media.AddOption(":no-audio");

            try
            {
                MediaPlayer.Mute = muted;
                MediaPlayer.Play(_media);
            }
            catch
            {
                Status = "Ошибка";
            }
        }

        public void Stop()
        {
            if (_isDisposed)
                return;

            try
            {
                MediaPlayer.Mute = true;
            }
            catch
            {
            }

            try
            {
                if (MediaPlayer.IsPlaying)
                    MediaPlayer.Stop();
            }
            catch
            {
            }
        }

        public void SafeShutdown()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            try { MediaPlayer.Opening -= MediaPlayer_Opening; } catch { }
            try { MediaPlayer.Buffering -= MediaPlayer_Buffering; } catch { }
            try { MediaPlayer.Playing -= MediaPlayer_Playing; } catch { }
            try { MediaPlayer.EncounteredError -= MediaPlayer_EncounteredError; } catch { }
            try { MediaPlayer.EndReached -= MediaPlayer_EndReached; } catch { }
            try { MediaPlayer.Stopped -= MediaPlayer_Stopped; } catch { }

            try
            {
                MediaPlayer.Mute = true;
            }
            catch
            {
            }

            try
            {
                MediaPlayer.Media = null;
            }
            catch
            {
            }

            try
            {
                if (MediaPlayer.IsPlaying)
                    MediaPlayer.Stop();
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
                MediaPlayer.Dispose();
            }
            catch
            {
            }
        }

        public void Dispose()
        {
            SafeShutdown();
        }

        private void MediaPlayer_Opening(object? sender, EventArgs e)
        {
            Status = "Подключение";
        }

        private void MediaPlayer_Buffering(object? sender, MediaPlayerBufferingEventArgs e)
        {
            Status = $"Буферизация {e.Cache:0}%";
        }

        private void MediaPlayer_Playing(object? sender, EventArgs e)
        {
            Status = "В эфире";
        }

        private void MediaPlayer_EncounteredError(object? sender, EventArgs e)
        {
            Status = "Ошибка";
        }

        private void MediaPlayer_EndReached(object? sender, EventArgs e)
        {
            Status = "Поток завершён";
        }

        private void MediaPlayer_Stopped(object? sender, EventArgs e)
        {
            Status = "Остановлено";
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}