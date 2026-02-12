using LibVLCSharp.Shared;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using CMVideo.Views.Controls;

namespace CMVideo.Views
{
    /// <summary>
    /// Interaction logic for PlayerWindow.xaml
    /// </summary>
    public partial class PlayerWindow : Window
    {
        readonly MediaControls _controls;

        // Fullscreen state
        private bool _isFullScreen;
        private WindowState _previousWindowState;
        private WindowStyle _previousWindowStyle;
        private ResizeMode _previousResizeMode;
        private double _previousTop;
        private double _previousLeft;
        private double _previousWidth;
        private double _previousHeight;

        /// <summary>
        /// Constructor for playing a single video file
        /// </summary>
        public PlayerWindow(string filePath) : this(new List<string> { filePath })
        {
        }

        /// <summary>
        /// Constructor for playing a playlist of video files
        /// </summary>
        public PlayerWindow(List<string> files)
        {
            InitializeComponent();

            _controls = new MediaControls(this, files);
            VideoView.Content = _controls;
        }

        private void Player_Loaded(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            window.KeyDown += HandleKeyPress;
        }

        private void HandleKeyPress(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Space:
                    _controls.PauseButton_Click(sender, e);
                    break;
                case Key.Left:
                    _controls.Rewind10_Click(sender, e);
                    break;
                case Key.Right:
                    _controls.Forward10_Click(sender, e);
                    break;
                case Key.F:
                    ToggleFullScreen();
                    break;
                case Key.Escape:
                    if (_isFullScreen)
                        ToggleFullScreen();
                    break;
            }
        }

        /// <summary>
        /// Toggle between fullscreen and windowed mode
        /// </summary>
        public void ToggleFullScreen()
        {
            if (_isFullScreen)
            {
                // Restore windowed mode
                WindowStyle = _previousWindowStyle;
                ResizeMode = _previousResizeMode;
                Topmost = false;
                WindowState = _previousWindowState;
                Top = _previousTop;
                Left = _previousLeft;
                Width = _previousWidth;
                Height = _previousHeight;
                _isFullScreen = false;
            }
            else
            {
                // Save current state
                _previousWindowState = WindowState;
                _previousWindowStyle = WindowStyle;
                _previousResizeMode = ResizeMode;
                _previousTop = Top;
                _previousLeft = Left;
                _previousWidth = Width;
                _previousHeight = Height;

                // Go fullscreen: borderless maximized
                WindowState = WindowState.Normal; // Reset first to avoid taskbar overlap
                WindowStyle = WindowStyle.None;
                ResizeMode = ResizeMode.NoResize;
                Topmost = true;
                WindowState = WindowState.Maximized;
                _isFullScreen = true;
            }
        }

        /// <summary>
        /// Handle double-click on the VideoView to toggle fullscreen
        /// </summary>
        private void VideoView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                ToggleFullScreen();
                e.Handled = true;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            VideoView.Dispose();
        }
    }
}
