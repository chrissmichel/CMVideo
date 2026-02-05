using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;

namespace CMVideo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ObservableCollection<VideoFileItem> _allVideos;
        private ObservableCollection<VideoFileItem> _filteredVideos;
        private string _currentFolderPath;

        // Supported video formats for LibVLC
        private readonly string[] _supportedVideoExtensions = new[]
        {
            ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm",
            ".m4v", ".mpg", ".mpeg", ".3gp", ".ogv", ".ts", ".m2ts"
        };

        public MainWindow()
        {
            InitializeComponent();
            _allVideos = new ObservableCollection<VideoFileItem>();
            _filteredVideos = new ObservableCollection<VideoFileItem>();
            VideosListView.ItemsSource = _filteredVideos;
        }

        /// <summary>
        /// Open folder button click handler
        /// </summary>
        private void OpenFolder_Button_Click(object sender, RoutedEventArgs e)
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Select a folder containing video files";
                folderDialog.ShowNewFolderButton = false;

                // Set initial directory to user's Videos folder
                string userName = GetUsername();
                if (!string.IsNullOrEmpty(userName))
                {
                    string videosPath = Path.Combine("C:\\Users", userName, "Videos");
                    if (Directory.Exists(videosPath))
                    {
                        folderDialog.SelectedPath = videosPath;
                    }
                }

                DialogResult result = folderDialog.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(folderDialog.SelectedPath))
                {
                    _currentFolderPath = folderDialog.SelectedPath;
                    LoadVideosFromFolder(_currentFolderPath);
                }
            }
        }

        /// <summary>
        /// Load all video files from the selected folder
        /// </summary>
        private void LoadVideosFromFolder(string folderPath)
        {
            _allVideos.Clear();
            _filteredVideos.Clear();

            try
            {
                var videoFiles = Directory.GetFiles(folderPath)
                    .Where(file => _supportedVideoExtensions.Contains(Path.GetExtension(file).ToLower()))
                    .OrderBy(file => Path.GetFileName(file));

                foreach (var videoFile in videoFiles)
                {
                    var videoItem = new VideoFileItem(videoFile);
                    _allVideos.Add(videoItem);
                    _filteredVideos.Add(videoItem);
                }

                if (_allVideos.Count == 0)
                {
                    System.Windows.MessageBox.Show(
                        "No video files found in the selected folder.",
                        "No Videos Found",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Error loading videos from folder: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Search box text changed event handler
        /// </summary>
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = SearchBox.Text.ToLower();
            _filteredVideos.Clear();

            var filtered = string.IsNullOrWhiteSpace(searchText)
                ? _allVideos
                : _allVideos.Where(v => v.FileName.ToLower().Contains(searchText));

            foreach (var video in filtered)
            {
                _filteredVideos.Add(video);
            }
        }

        /// <summary>
        /// Play button click handler for individual video items
        /// </summary>
        private void PlayVideo_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is string filePath)
            {
                OpenPlayerWithVideo(filePath);
            }
        }

        /// <summary>
        /// ListView double-click handler
        /// </summary>
        private void VideosListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (VideosListView.SelectedItem is VideoFileItem selectedVideo)
            {
                OpenPlayerWithVideo(selectedVideo.FilePath);
            }
        }

        /// <summary>
        /// Opens the Player window with the specified video file
        /// </summary>
        private void OpenPlayerWithVideo(string filePath)
        {
            try
            {
                var player = new Player(filePath);
                player.Show();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Error opening video player: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Exit menu item click handler
        /// </summary>
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        /// <summary>
        /// Get the current Windows username
        /// </summary>
        private string GetUsername()
        {
            try
            {
                string userName = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
                int slashIndex = userName.IndexOf('\\');
                if (slashIndex >= 0 && slashIndex < userName.Length - 1)
                {
                    return userName.Substring(slashIndex + 1);
                }
                return userName;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
 
