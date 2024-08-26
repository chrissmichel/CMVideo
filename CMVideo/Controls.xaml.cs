using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using LibVLCSharp.Shared;
using LibVLCSharp.WPF;
using MaterialDesignThemes.Wpf;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;

namespace CMVideo
{
    public partial class Controls : UserControl
    {
        readonly Player parent;
        LibVLC _libVLC;
        MediaPlayer _mediaPlayer;
        List<string> files;
        string file_path;
        int filecount;
        private readonly DispatcherTimer _timer;
        private bool _isDraggingSlider;
        private bool _endReached = false;
        
        
        public Controls(Player Parent, List<string> files)
        {
            parent = Parent;
            this.files = files;
            filecount = 0;
            file_path = files.Count > 0 ? files[0] : null;
            InitializeComponent();
            Core.Initialize();
            Parent.VideoView.Loaded += VideoView_Loaded;
            PlayButton.Click += PlayButton_Click;
            StopButton.Click += StopButton_Click;
            Unloaded += Controls_Unloaded;
            PauseButton.Click += PauseButton_Click;
            
             

          
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(25);
            _timer.Tick += Timer_Tick;

            if (_mediaPlayer != null)
            {
                _mediaPlayer.Volume = (int)Volume.Value;
                _mediaPlayer.EndReached += MediaPlayer_EndReached;

            }
           
            videoSlider.AddHandler(Slider.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(VideoSlider_DragStarted), true);
            videoSlider.AddHandler(Slider.PreviewMouseLeftButtonUpEvent, new MouseButtonEventHandler(VideoSlider_DragCompleted), true);
            videoSlider.ValueChanged += VideoSlider_ValueChanged;

        }

        private void MediaPlayer_EndReached(object sender, EventArgs e)
        {
            Console.WriteLine("EndReached event fired"); // Debug output

            Dispatcher.InvokeAsync(() =>
            {
              

                if (filecount < files.Count - 1)
                {
                    filecount++;
                    string nextVideoPath = files[filecount];
                    var media = new Media(_libVLC, new Uri(nextVideoPath));
                    _mediaPlayer.Media = media;
                    _mediaPlayer.Play();
                }
                else
                {
                    MessageBox.Show("All videos in the playlist have been played.");
                }
            });
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_mediaPlayer.IsPlaying)
            {
                PauseButton.Content = new MaterialDesignThemes.Wpf.PackIcon
                {
                    Kind = PackIconKind.PauseBox
                };
           
            }
            else if (!_mediaPlayer.IsPlaying)
            {
                PauseButton.Content = new MaterialDesignThemes.Wpf.PackIcon
                {
                    Kind = PackIconKind.PlayBox
                };
     
            }
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

        public void meme(object senter, EventArgs e)
        {
            if (_mediaPlayer.IsPlaying)
            {
                _mediaPlayer.Pause();
            }

            _mediaPlayer.Time += (long)41.67;
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
            _mediaPlayer.EndReached += MediaPlayer_EndReached;
     
            PlayButton_Click(sender, e);
            // Subscribe to the EndReached event here
       

        }


        void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (_mediaPlayer.IsPlaying)
            {
                _mediaPlayer.Stop();
            }
            _timer.Stop();
        }

        public void PauseButton_Click(object sender, RoutedEventArgs e)
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

        public void Forward10_Click(object sender, RoutedEventArgs e)
        {
            SeekTo(TimeSpan.FromMilliseconds(_mediaPlayer.Time) + TimeSpan.FromSeconds(10));
        }

        public void Rewind10_Click(object sender, RoutedEventArgs e)
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