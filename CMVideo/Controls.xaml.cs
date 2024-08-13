using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
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
            parent = Parent; 
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
         
           
              _mediaPlayer.Volume = (int)Volume.Value;
            }

        }

 
        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_mediaPlayer != null && _mediaPlayer.Length > 0)
            {
                Dispatcher.Invoke(() =>
                {
                    if (!_isDragingSlidder) // Update slider only if not dragging
                    {
                        Console.WriteLine("Media duration = " + _mediaPlayer.Media.Duration);
                        Console.WriteLine("Media player time = " + _mediaPlayer.Time);
                        double i = ((double) _mediaPlayer.Time / (double) _mediaPlayer.Media.Duration);
                        Console.WriteLine(i);
                        Timestamp.Text  = string.Format("{0:mm\\:ss}", TimeSpan.FromMilliseconds(_mediaPlayer.Time));

                        videoSlider.Value = i;
                   
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

 
           // string packUri = _mediaPlayer.Media.Meta(MetadataType.ArtworkURL);

           
          //  BitmapImage bit = new BitmapImage();
            //bit.BeginInit();
         //   bit.UriSource = new Uri(packUri, UriKind.Absolute);
           // bit.EndInit();
        //    ImageBox.Source = bit;
        }

        void PlayButton_Click(object sender, RoutedEventArgs e)
        {
        if(string.IsNullOrEmpty(file_path))
        { 
             file_path = "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ElephantsDream.mp4";
        }
        var media = new Media(_libVLC, new Uri(file_path));
        _mediaPlayer.Play(media);


        _timer.Start();

        }

        private void Volume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if(_mediaPlayer != null)
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
            SeekTo(TimeSpan.FromMilliseconds(_mediaPlayer.Time) - TimeSpan.FromSeconds(10));
        }

        void SeekTo(TimeSpan seconds)
        {
            _mediaPlayer.Time = (long)seconds.TotalMilliseconds;
        }

    }
}