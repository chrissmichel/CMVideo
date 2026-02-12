using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LibVLCSharp.Shared;
using LibVLCSharp.WPF;
using CMVideo.ViewModels;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;

namespace CMVideo.Views.Controls
{
    public partial class MediaControls : UserControl
    {
        private readonly PlayerWindow _parent;
        private MediaPlayerViewModel _viewModel;
        private LibVLC _libVlc;
        private MediaPlayer _mediaPlayer;

        public MediaControls(PlayerWindow parent, List<string> files)
        {
            _parent = parent ?? throw new ArgumentNullException(nameof(parent));

            InitializeComponent();
            Core.Initialize();

            // Setup VideoView loaded event
            _parent.VideoView.Loaded += VideoView_Loaded;
            Unloaded += MediaControls_Unloaded;

            // Store files for later initialization
            this.Tag = files; // Temporary storage until VideoView loads
        }

        private void VideoView_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize LibVLC and MediaPlayer
            _libVlc = new LibVLC(enableDebugLogs: true);
            _mediaPlayer = new MediaPlayer(_libVlc);
            _parent.VideoView.MediaPlayer = _mediaPlayer;

            // Get the playlist from Tag
            var files = this.Tag as List<string>;
            if (files == null || files.Count == 0)
            {
                files = new List<string> { "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ElephantsDream.mp4" };
            }

            // Initialize ViewModel with the media player
            _viewModel = new MediaPlayerViewModel(files, _libVlc, _mediaPlayer);

            // Set DataContext for bindings
            this.DataContext = _viewModel;

            // Setup slider drag events (can't be bound directly in XAML)
            VideoSlider.PreviewMouseLeftButtonDown += VideoSlider_PreviewMouseLeftButtonDown;
            VideoSlider.PreviewMouseLeftButtonUp += VideoSlider_PreviewMouseLeftButtonUp;
            VideoSlider.ValueChanged += VideoSlider_ValueChanged;
        }

        private void VideoSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _viewModel?.SliderDragStartedCommand.Execute(null);
        }

        private void VideoSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _viewModel?.SliderDragCompletedCommand.Execute(null);
        }

        private void VideoSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Update timestamp preview while dragging
            _viewModel?.UpdateSliderPreview(e.NewValue);
        }

        private void MediaControls_Unloaded(object sender, RoutedEventArgs e)
        {
            // Clean up resources
            _viewModel?.Dispose();
            _mediaPlayer?.Stop();
            _mediaPlayer?.Dispose();
            _libVlc?.Dispose();
        }

        // Public methods for Player.xaml.cs keyboard shortcuts
        public void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel?.PlayPauseCommand.Execute(null);
        }

        public void Rewind10_Click(object sender, RoutedEventArgs e)
        {
            _viewModel?.RewindCommand.Execute(null);
        }

        public void Forward10_Click(object sender, RoutedEventArgs e)
        {
            _viewModel?.ForwardCommand.Execute(null);
        }
    }
}
