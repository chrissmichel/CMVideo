using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using LibVLCSharp.Shared;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;

namespace CMVideo
{
    /// <summary>
    /// Interaction logic for Controls.xaml
    /// </summary>
    public partial class Controls : UserControl
    {
        readonly Player parent;
        LibVLC _libVLC;
        MediaPlayer _mediaPlayer;
        string file_path;
        private DispatcherTimer _timer;
        private bool _isDragingSlidder;

        public Controls(Player Parent, string file_path)
        {
            InitializeComponent();
            Core.Initialize();
            // we need the VideoView to be fully loaded before setting a MediaPlayer on it.
            Parent.VideoView.Loaded += VideoView_Loaded;
            // Initialize buttons
            PlayButton.Click += PlayButton_Click;
            StopButton.Click += StopButton_Click;
            Unloaded += Controls_Unloaded;
            PauseButton.Click += PauseButton_Click;
            this.file_path = file_path;

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(500);
            _timer.Tick += Timer_Tick;

            if(_mediaPlayer != null)
            {
              _mediaPlayer.LengthChanged += MediaPlayer_LengthChanged;
              _mediaPlayer.TimeChanged += MediaPlayer_TimeChanged;
              _mediaPlayer.Volume = (int)Volume.Value;
            }

        }

        private void MediaPlayer_LengthChanged(object sender, MediaPlayerLengthChangedEventArgs e)
        {
            Dispatcher.Invoke(() => videoSlider.Maximum = e.Length / 1000.0);
        }

        private void MediaPlayer_TimeChanged(object sender, MediaPlayerTimeChangedEventArgs e)
        {
            Dispatcher.Invoke(() => videoSlider.Value = e.Time / 1000.0);
        }

        private void VideoSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_mediaPlayer != null && _mediaPlayer.Length > 0 && Math.Abs(_mediaPlayer.Time / 1000.0 - e.NewValue) > 1.0)
            {
                _mediaPlayer.Time = (long)(e.NewValue * 1000);
            }
        }

        private void FullScreen_Click(object sender, RoutedEventArgs e)
        {
            _mediaPlayer.ToggleFullscreen();
            Console.WriteLine(_mediaPlayer.Fullscreen);
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_mediaPlayer != null && _mediaPlayer.Length > 0)
            {
                Dispatcher.Invoke(() =>
                {
                    if (!_isDragingSlidder) // Update slider only if not dragging
                    {
                        videoSlider.Value = _mediaPlayer.Time / 1000.0;
                    }
                });
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
        }

        void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (parent.VideoView.MediaPlayer.IsPlaying)
            {
                parent.VideoView.MediaPlayer.Stop();
            }
            _timer.Stop();
        }

        void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            if(parent.VideoView.MediaPlayer.IsPlaying)
            {
                parent.VideoView.MediaPlayer.Pause();
                PauseButton.Background = Brushes.Crimson;
            }
            else
            {
                parent.VideoView.MediaPlayer.Play();
                PauseButton.Background = Brushes.Green;
            }
            _timer.Stop();
        }
        
        void PlayButton_Click(object sender, RoutedEventArgs e)
        {
        if(file_path == null || file_path == "")
           {
                file_path = "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ElephantsDream.mp4";
            }
        if (!parent.VideoView.MediaPlayer.IsPlaying)
        {
            using (var media = new Media(_libVLC, new Uri(file_path)))
            parent.VideoView.MediaPlayer.Play(media);
        }
        _timer.Start();
        }
        private void Volume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _mediaPlayer.Volume = (int)e.NewValue;
        }
    }
}