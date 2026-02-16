using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using CMVideo.Models;
using CMVideo.Services;

namespace CMVideo.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<VideoFileItem> _allVideos;
        private readonly ObservableCollection<VideoFileItem> _filteredVideos;
        private readonly ObservableCollection<VideoFileItem> _currentPageVideos;
        private string _currentFolderPath;
        private readonly ThumbnailService _thumbnailService;
        private bool _isGridView = true;

        // Pagination fields
        private int _currentPage = 1;
        private int _pageSize = 50;
        private int _totalPages = 0;
        private readonly SemaphoreSlim _thumbnailSemaphore = new SemaphoreSlim(3, 3); // Max 3 concurrent thumbnail operations
        private CancellationTokenSource _thumbnailCancellationTokenSource;

        // Supported video formats for LibVLC
        private readonly string[] _supportedVideoExtensions = new []
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
            _currentPageVideos = new ObservableCollection<VideoFileItem>();
            _thumbnailService = new ThumbnailService();
            VideosListView.ItemsSource = _currentPageVideos;

            // Hide pagination controls initially
            UpdatePaginationVisibility(false);
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
            _currentPageVideos.Clear();

            // Cancel any pending thumbnail operations
            _thumbnailCancellationTokenSource?.Cancel();
            _thumbnailCancellationTokenSource = new CancellationTokenSource();

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
                }

                if (_allVideos.Count == 0)
                {
                    System.Windows.MessageBox.Show(
                        "No media files found in the selected folder.",
                        "No Media Found",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    UpdatePaginationVisibility(false);
                    return;
                }

                // Initialize pagination
                _currentPage = 1;
                CalculateTotalPages();
                LoadCurrentPage();
                UpdatePaginationUI();
                UpdatePaginationVisibility(true);

                // Load thumbnails for current page only
                await LoadThumbnailsForCurrentPageAsync();
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
        private async Task GenerateThumbnailAsync(VideoFileItem mediaItem, CancellationToken cancellationToken)
        {
            try
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                var thumbnail = await _thumbnailService.GenerateThumbnailAsync(mediaItem, cancellationToken);
                if (!cancellationToken.IsCancellationRequested)
                {
                    // Update on UI thread
                    Dispatcher.Invoke(() =>
                    {
                        mediaItem.Thumbnail = thumbnail;
                    });
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when page changes
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating thumbnail: {ex.Message}");
            }
        }

        /// <summary>
        /// Search box text changed event handler
        /// </summary>
        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
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

            // Reset to page 1 when search changes
            if (_filteredVideos.Count > 0)
            {
                _currentPage = 1;
                CalculateTotalPages();
                LoadCurrentPage();
                UpdatePaginationUI();
                UpdatePaginationVisibility(true);
                await LoadThumbnailsForCurrentPageAsync();
            }
            else
            {
                _currentPageVideos.Clear();
                UpdatePaginationVisibility(false);
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
                var player = new PlayerWindow(filePath);
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

        #region Pagination Methods

        /// <summary>
        /// Calculate total pages based on filtered videos and page size
        /// </summary>
        private void CalculateTotalPages()
        {
            _totalPages = (int)Math.Ceiling((double)_filteredVideos.Count / _pageSize);
            if (_totalPages == 0) _totalPages = 1;
        }

        /// <summary>
        /// Load videos for the current page into the display collection
        /// </summary>
        private void LoadCurrentPage()
        {
            // Cancel any pending thumbnail operations
            _thumbnailCancellationTokenSource?.Cancel();
            _thumbnailCancellationTokenSource = new CancellationTokenSource();

            // Clear thumbnails from previous page to free memory
            foreach (var item in _currentPageVideos)
            {
                item.Thumbnail = null;
            }

            _currentPageVideos.Clear();

            int startIndex = (_currentPage - 1) * _pageSize;
            int endIndex = Math.Min(startIndex + _pageSize, _filteredVideos.Count);

            for (int i = startIndex; i < endIndex; i++)
            {
                _currentPageVideos.Add(_filteredVideos[i]);
            }
        }

        /// <summary>
        /// Load thumbnails for videos on the current page with throttling
        /// </summary>
        private async Task LoadThumbnailsForCurrentPageAsync()
        {
            var cancellationToken = _thumbnailCancellationTokenSource.Token;
            var tasks = new List<Task>();

            foreach (var mediaItem in _currentPageVideos.ToList())
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                tasks.Add(Task.Run(async () =>
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;

                    await _thumbnailSemaphore.WaitAsync(cancellationToken);
                    try
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await GenerateThumbnailAsync(mediaItem, cancellationToken);
                        }
                    }
                    finally
                    {
                        _thumbnailSemaphore.Release();
                    }
                }, cancellationToken));
            }

            try
            {
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException)
            {
                // Expected when page changes
            }
        }

        /// <summary>
        /// Update pagination UI elements
        /// </summary>
        private void UpdatePaginationUI()
        {
            if (PageInfoTextBlock != null)
            {
                PageInfoTextBlock.Text = $"Page {_currentPage} of {_totalPages}";
            }

            if (TotalItemsTextBlock != null)
            {
                TotalItemsTextBlock.Text = $"{_filteredVideos.Count} items";
            }

            if (FirstPageButton != null)
            {
                FirstPageButton.IsEnabled = _currentPage > 1;
            }

            if (PreviousPageButton != null)
            {
                PreviousPageButton.IsEnabled = _currentPage > 1;
            }

            if (NextPageButton != null)
            {
                NextPageButton.IsEnabled = _currentPage < _totalPages;
            }

            if (LastPageButton != null)
            {
                LastPageButton.IsEnabled = _currentPage < _totalPages;
            }
        }

        /// <summary>
        /// Show or hide pagination controls
        /// </summary>
        private void UpdatePaginationVisibility(bool visible)
        {
            var visibility = visible ? Visibility.Visible : Visibility.Collapsed;

            if (PaginationToolbar != null)
            {
                PaginationToolbar.Visibility = visibility;
            }
        }

        /// <summary>
        /// First page button click handler
        /// </summary>
        private async void FirstPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage != 1)
            {
                _currentPage = 1;
                LoadCurrentPage();
                UpdatePaginationUI();
                await LoadThumbnailsForCurrentPageAsync();
            }
        }

        /// <summary>
        /// Previous page button click handler
        /// </summary>
        private async void PreviousPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                LoadCurrentPage();
                UpdatePaginationUI();
                await LoadThumbnailsForCurrentPageAsync();
            }
        }

        /// <summary>
        /// Next page button click handler
        /// </summary>
        private async void NextPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                LoadCurrentPage();
                UpdatePaginationUI();
                await LoadThumbnailsForCurrentPageAsync();
            }
        }

        /// <summary>
        /// Last page button click handler
        /// </summary>
        private async void LastPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage != _totalPages)
            {
                _currentPage = _totalPages;
                LoadCurrentPage();
                UpdatePaginationUI();
                await LoadThumbnailsForCurrentPageAsync();
            }
        }

        /// <summary>
        /// Page size selection changed handler
        /// </summary>
        private async void PageSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PageSizeComboBox?.SelectedItem is ComboBoxItem selectedItem &&
                int.TryParse(selectedItem.Content.ToString(), out int newPageSize))
            {
                if (newPageSize != _pageSize && _filteredVideos.Count > 0)
                {
                    _pageSize = newPageSize;
                    _currentPage = 1;
                    CalculateTotalPages();
                    LoadCurrentPage();
                    UpdatePaginationUI();
                    await LoadThumbnailsForCurrentPageAsync();
                }
            }
        }

        #endregion
    }
}
 
