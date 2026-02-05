using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
        private ThumbnailService _thumbnailService;
        private bool _isGridView = true;

        // Supported video formats for LibVLC
        private readonly string[] _supportedVideoExtensions = new[]
        {
            ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm",
            ".m4v", ".mpg", ".mpeg", ".3gp", ".ogv", ".ts", ".m2ts"
        };

        // Supported audio formats
        private readonly string[] _supportedAudioExtensions = new[]
        {
            ".mp3", ".wav", ".flac", ".m4a", ".aac", ".ogg", ".wma", ".opus"
        };

        public MainWindow()
        {
            InitializeComponent();
            _allVideos = new ObservableCollection<VideoFileItem>();
            _filteredVideos = new ObservableCollection<VideoFileItem>();
            _thumbnailService = new ThumbnailService();
            VideosItemsControl.ItemsSource = _filteredVideos;
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
        /// Load all media files (video and audio) from the selected folder
        /// </summary>
        private async void LoadVideosFromFolder(string folderPath)
        {
            _allVideos.Clear();
            _filteredVideos.Clear();

            try
            {
                var allSupportedExtensions = _supportedVideoExtensions.Concat(_supportedAudioExtensions).ToArray();
                var mediaFiles = Directory.GetFiles(folderPath)
                    .Where(file => allSupportedExtensions.Contains(Path.GetExtension(file).ToLower()))
                    .OrderBy(file => Path.GetFileName(file));

                foreach (var mediaFile in mediaFiles)
                {
                    var mediaItem = new VideoFileItem(mediaFile);
                    _allVideos.Add(mediaItem);
                    _filteredVideos.Add(mediaItem);

                    // Generate thumbnail asynchronously
                    _ = GenerateThumbnailAsync(mediaItem);
                }

                if (_allVideos.Count == 0)
                {
                    System.Windows.MessageBox.Show(
                        "No media files found in the selected folder.",
                        "No Media Found",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Error loading media from folder: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Generate thumbnail for a media item asynchronously
        /// </summary>
        private async Task GenerateThumbnailAsync(VideoFileItem mediaItem)
        {
            try
            {
                var thumbnail = await _thumbnailService.GenerateThumbnailAsync(mediaItem);
                if (thumbnail != null)
                {
                    // Update on UI thread
                    Dispatcher.Invoke(() =>
                    {
                        mediaItem.Thumbnail = thumbnail;
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating thumbnail: {ex.Message}");
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
        /// Video card click handler
        /// </summary>
        private void VideoCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is string filePath)
            {
                OpenPlayerWithVideo(filePath);
            }
        }

        /// <summary>
        /// View toggle button click handler (Grid/List view)
        /// </summary>
        private void ViewToggle_Click(object sender, RoutedEventArgs e)
        {
            _isGridView = !_isGridView;
            // TODO: Implement list view template switching if needed
            // For now, just update the icon
            if (ViewToggleButton.Content is MaterialDesignThemes.Wpf.PackIcon icon)
            {
                icon.Kind = _isGridView
                    ? MaterialDesignThemes.Wpf.PackIconKind.ViewGrid
                    : MaterialDesignThemes.Wpf.PackIconKind.ViewList;
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
 
