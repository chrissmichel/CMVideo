using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using LibVLCSharp.Shared;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;

namespace CMVideo
{
    public partial class Controls : UserControl
    {
        readonly Player parent;
        LibVLC _libVLC;
        MediaPlayer _mediaPlayer;
        string file_path;
        private readonly DispatcherTimer _timer;
        private bool _isDraggingSlider;

        public Controls(Player Parent, string file_path)
        {
            parent = Parent;
            InitializeComponent();
            Core.Initialize();
            Parent.VideoView.Loaded += VideoView_Loaded;
            PlayButton.Click += PlayButton_Click;
            StopButton.Click += StopButton_Click;
            Unloaded += Controls_Unloaded;
            PauseButton.Click += PauseButton_Click;
            this.file_path = file_path;

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(100);
            _timer.Tick += Timer_Tick;

            if (_mediaPlayer != null)
            {
                _mediaPlayer.Volume = (int)Volume.Value;
            }

            videoSlider.AddHandler(Slider.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(VideoSlider_DragStarted), true);
            videoSlider.AddHandler(Slider.PreviewMouseLeftButtonUpEvent, new MouseButtonEventHandler(VideoSlider_DragCompleted), true);
            videoSlider.ValueChanged += VideoSlider_ValueChanged;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_mediaPlayer != null && _mediaPlayer.Length > 0)
            {
                Dispatcher.Invoke(() =>
                {
                    if (!_isDraggingSlider) // Update slider only if not dragging
                    {
                        videoSlider.Value = (double)_mediaPlayer.Time / _mediaPlayer.Length;
                    }
                });
            }
            Timestamp.Content = string.Format("{0:mm\\:ss}", TimeSpan.FromMilliseconds(_mediaPlayer.Time));
        }

        private void VideoSlider_DragStarted(object sender, MouseButtonEventArgs e)
        {
            _isDraggingSlider = true;
        }

        private void VideoSlider_DragCompleted(object sender, MouseButtonEventArgs e)
        {
            _isDraggingSlider = false;
            SeekTo(TimeSpan.FromMilliseconds(videoSlider.Value * _mediaPlayer.Length));
        }

        private void VideoSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isDraggingSlider)
            {
                Timestamp.Content = string.Format("{0:mm\\:ss}", TimeSpan.FromMilliseconds(e.NewValue * _mediaPlayer.Length));
            }
        }

        private void Controls_Unloaded(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            _mediaPlayer.Stop();
            _mediaPlayer.Dispose();
            _libVLC.Dispose();
        }

        private void VideoView_Loaded(object sender, RoutedEventArgs e)
        {
            _libVLC = new LibVLC(enableDebugLogs: true);
            _mediaPlayer = new MediaPlayer(_libVLC);
            parent.VideoView.MediaPlayer = _mediaPlayer;
            _mediaPlayer.Volume = (int)Volume.Value;

        }

        void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (_mediaPlayer.IsPlaying)
            {
                _mediaPlayer.Stop();
            }
            _timer.Stop();
        }

        void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            _mediaPlayer.Pause();
        }

        void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(file_path))
            {
                file_path = "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ElephantsDream.mp4";
            }
            var media = new Media(_libVLC, new Uri(file_path));
            _mediaPlayer.Play(media);
  
            _timer.Start();
        }

        private void Volume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Volume = (int)e.NewValue;
            }
        }

        private void Forward10_Click(object sender, RoutedEventArgs e)
        {
            SeekTo(TimeSpan.FromMilliseconds(_mediaPlayer.Time) + TimeSpan.FromSeconds(10));
        }

        private void Rewind10_Click(object sender, RoutedEventArgs e)
        {
            if (_mediaPlayer != null )
            {
                if (TimeSpan.FromMilliseconds(_mediaPlayer.Time) < TimeSpan.FromSeconds(10))
                {
                    SeekTo(TimeSpan.FromSeconds(0));
                    return;
                }
                SeekTo(TimeSpan.FromMilliseconds(_mediaPlayer.Time) - TimeSpan.FromSeconds(10));
            }
        }

        void SeekTo(TimeSpan time)
        {
            _mediaPlayer.Time = (long)time.TotalMilliseconds;
        }
    }
}