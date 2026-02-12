using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using LibVLCSharp.Shared;
using MaterialDesignThemes.Wpf;

namespace CMVideo
{
    /// <summary>
    /// ViewModel for media player controls following MVVM pattern
    /// </summary>
    public class MediaPlayerViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly LibVLC _libVLC;
        private readonly MediaPlayer _mediaPlayer;
        private readonly DispatcherTimer _timer;
        private readonly List<string> _playlist;
        private int _currentFileIndex;
        
        // Cached values for change detection
        private bool _lastIsPlayingState;
        private bool _lastIsLoopingState;

        // Backing fields
        private double _position;
        private string _timestamp = "00:00";
        private int _volume = 50;
        private bool _isLooping;
        private bool _isDraggingSlider;
        private PackIconKind _playPauseIcon = PackIconKind.PauseBox;
        private PackIconKind _repeatIcon = PackIconKind.RepeatOff;

        public event PropertyChangedEventHandler PropertyChanged;

        #region Properties

        public MediaPlayer MediaPlayer => _mediaPlayer;

        public double Position
        {
            get => _position;
            set
            {
                if (Math.Abs(_position - value) > 0.001) // Only update if changed significantly
                {
                    _position = Math.Max(0.0, Math.Min(1.0, value)); // Clamp between 0 and 1
                    OnPropertyChanged();
                }
            }
        }

        public string Timestamp
        {
            get => _timestamp;
            private set
            {
                if (_timestamp != value)
                {
                    _timestamp = value;
                    OnPropertyChanged();
                }
            }
        }

        public int Volume
        {
            get => _volume;
            set
            {
                if (_volume != value)
                {
                    _volume = Math.Max(0, Math.Min(100, value)); // Clamp between 0 and 100
                    if (_mediaPlayer != null)
                    {
                        _mediaPlayer.Volume = _volume;
                    }
                    OnPropertyChanged();
                }
            }
        }

        public bool IsLooping
        {
            get => _isLooping;
            set
            {
                if (_isLooping != value)
                {
                    _isLooping = value;
                    OnPropertyChanged();
                    UpdateRepeatIcon();
                }
            }
        }

        public bool IsDraggingSlider
        {
            get => _isDraggingSlider;
            set
            {
                if (_isDraggingSlider != value)
                {
                    _isDraggingSlider = value;
                    OnPropertyChanged();
                }
            }
        }

        public PackIconKind PlayPauseIcon
        {
            get => _playPauseIcon;
            private set
            {
                if (_playPauseIcon != value)
                {
                    _playPauseIcon = value;
                    OnPropertyChanged();
                }
            }
        }

        public PackIconKind RepeatIcon
        {
            get => _repeatIcon;
            private set
            {
                if (_repeatIcon != value)
                {
                    _repeatIcon = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region Commands

        public ICommand PlayPauseCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand ForwardCommand { get; }
        public ICommand RewindCommand { get; }
        public ICommand ToggleLoopCommand { get; }
        public ICommand SliderDragStartedCommand { get; }
        public ICommand SliderDragCompletedCommand { get; }

        #endregion

        public MediaPlayerViewModel(List<string> playlist, LibVLC libVLC, MediaPlayer mediaPlayer)
        {
            _playlist = playlist ?? throw new ArgumentNullException(nameof(playlist));
            _libVLC = libVLC ?? throw new ArgumentNullException(nameof(libVLC));
            _mediaPlayer = mediaPlayer ?? throw new ArgumentNullException(nameof(mediaPlayer));
            _currentFileIndex = 0;

            // Initialize commands
            PlayPauseCommand = new RelayCommand(PlayPause);
            StopCommand = new RelayCommand(Stop);
            ForwardCommand = new RelayCommand(Forward);
            RewindCommand = new RelayCommand(Rewind);
            ToggleLoopCommand = new RelayCommand(ToggleLoop);
            SliderDragStartedCommand = new RelayCommand(OnSliderDragStarted);
            SliderDragCompletedCommand = new RelayCommand(OnSliderDragCompleted);

            // Configure media player
            _mediaPlayer.Volume = _volume;
            _mediaPlayer.EndReached += MediaPlayer_EndReached;

            // Initialize timer with optimized interval (100ms instead of 25ms = 75% less CPU)
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _timer.Tick += Timer_Tick;

            // Start playing first file
            if (_playlist.Count > 0)
            {
                PlayFile(_playlist[_currentFileIndex]);
                _timer.Start();
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_mediaPlayer == null || _mediaPlayer.Length <= 0)
                return;

            // Only update icons if state has changed (optimization)
            bool isPlaying = _mediaPlayer.IsPlaying;
            if (isPlaying != _lastIsPlayingState)
            {
                _lastIsPlayingState = isPlaying;
                UpdatePlayPauseIcon();
            }

            if (_isLooping != _lastIsLoopingState)
            {
                _lastIsLoopingState = _isLooping;
                UpdateRepeatIcon();
            }

            // Update position and timestamp
            if (!IsDraggingSlider)
            {
                double newPosition = (double)_mediaPlayer.Time / _mediaPlayer.Length;
                Position = newPosition;
            }

            string newTimestamp = TimeSpan.FromMilliseconds(_mediaPlayer.Time).ToString(@"mm\:ss");
            Timestamp = newTimestamp;
        }

        private void MediaPlayer_EndReached(object sender, EventArgs e)
        {
            Application.Current?.Dispatcher.InvokeAsync(() =>
            {
                if (IsLooping)
                {
                    // Fix: Properly restart the current media
                    if (_mediaPlayer.Media != null)
                    {
                        _mediaPlayer.Stop();
                        _mediaPlayer.Media.Dispose();
                    }
                    
                    // Recreate and play the current file
                    var media = new Media(_libVLC, new Uri(_playlist[_currentFileIndex]));
                    _mediaPlayer.Media = media;
                    _mediaPlayer.Play();
                }
                else if (_currentFileIndex < _playlist.Count - 1)
                {
                    // Play next file in playlist
                    _currentFileIndex++;
                    PlayFile(_playlist[_currentFileIndex]);
                }
                else
                {
                    // End of playlist
                    Stop();
                }
            });
        }

        private void PlayFile(string filePath)
        {
            if (_mediaPlayer.Media != null)
            {
                _mediaPlayer.Stop();
                _mediaPlayer.Media.Dispose();
            }

            var media = new Media(_libVLC, new Uri(filePath));
            _mediaPlayer.Media = media;
            _mediaPlayer.Play();
        }

        private void PlayPause()
        {
            if (_mediaPlayer.IsPlaying)
            {
                _mediaPlayer.Pause();
            }
            else
            {
                if (_mediaPlayer.Media == null && _playlist.Count > 0)
                {
                    PlayFile(_playlist[_currentFileIndex]);
                }
                else
                {
                    _mediaPlayer.Play();
                }
            }
            UpdatePlayPauseIcon();
        }

        private void Stop()
        {
            if (_mediaPlayer.IsPlaying)
            {
                _mediaPlayer.Stop();
            }
            _timer.Stop();
            Position = 0;
            Timestamp = "00:00";
            UpdatePlayPauseIcon();
        }

        private void Forward()
        {
            if (_mediaPlayer != null && _mediaPlayer.Length > 0)
            {
                long newTime = Math.Min(_mediaPlayer.Time + 10000, _mediaPlayer.Length);
                _mediaPlayer.Time = newTime;
            }
        }

        private void Rewind()
        {
            if (_mediaPlayer != null && _mediaPlayer.Length > 0)
            {
                long newTime = Math.Max(_mediaPlayer.Time - 10000, 0);
                _mediaPlayer.Time = newTime;
            }
        }

        private void ToggleLoop()
        {
            IsLooping = !IsLooping;
        }

        private void OnSliderDragStarted()
        {
            IsDraggingSlider = true;
        }

        private void OnSliderDragCompleted()
        {
            IsDraggingSlider = false;
            if (_mediaPlayer != null && _mediaPlayer.Length > 0)
            {
                long newTime = (long)(Position * _mediaPlayer.Length);
                _mediaPlayer.Time = newTime;
            }
        }

        public void UpdateSliderPreview(double position)
        {
            if (IsDraggingSlider && _mediaPlayer != null && _mediaPlayer.Length > 0)
            {
                // Update timestamp preview while dragging
                long previewTime = (long)(position * _mediaPlayer.Length);
                Timestamp = TimeSpan.FromMilliseconds(previewTime).ToString(@"mm\:ss");
            }
        }

        private void UpdatePlayPauseIcon()
        {
            PlayPauseIcon = _mediaPlayer?.IsPlaying == true ? PackIconKind.PauseBox : PackIconKind.PlayBox;
        }

        private void UpdateRepeatIcon()
        {
            RepeatIcon = IsLooping ? PackIconKind.RepeatOnce : PackIconKind.RepeatOff;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            _timer?.Stop();
            if (_mediaPlayer != null)
            {
                _mediaPlayer.EndReached -= MediaPlayer_EndReached;
                _mediaPlayer.Stop();
            }
        }
    }
}
