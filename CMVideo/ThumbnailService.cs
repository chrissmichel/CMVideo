using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;

namespace CMVideo
{
    public class ThumbnailService
    {
        private readonly string _thumbnailCacheFolder;
        private readonly string[] _commonAlbumArtNames = { "folder.jpg", "cover.jpg", "album.jpg", "front.jpg", "artwork.jpg", "folder.png", "cover.png", "album.png" };

        public ThumbnailService()
        {
            // Create thumbnail cache folder in AppData
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _thumbnailCacheFolder = Path.Combine(appDataPath, "CMVideo", "Thumbnails");

            if (!Directory.Exists(_thumbnailCacheFolder))
            {
                Directory.CreateDirectory(_thumbnailCacheFolder);
            }
        }

        /// <summary>
        /// Generate thumbnail for a media file (video or audio)
        /// </summary>
        public async Task<BitmapImage> GenerateThumbnailAsync(VideoFileItem mediaItem, CancellationToken cancellationToken = default)
        {
            if (mediaItem == null || string.IsNullOrEmpty(mediaItem.FilePath))
                return null;

            try
            {
                mediaItem.IsLoadingThumbnail = true;
                cancellationToken.ThrowIfCancellationRequested();

                // Check if thumbnail already exists in cache
                string cachedThumbnail = GetCachedThumbnailPath(mediaItem.FilePath);
                if (File.Exists(cachedThumbnail))
                {
                    mediaItem.ThumbnailPath = cachedThumbnail;
                    return await LoadBitmapImageAsync(cachedThumbnail);
                }

                cancellationToken.ThrowIfCancellationRequested();

                // Generate thumbnail based on media type
                string thumbnailPath = null;
                switch (mediaItem.MediaType)
                {
                    case MediaType.Video:
                        thumbnailPath = await ExtractVideoThumbnailAsync(mediaItem.FilePath, cachedThumbnail);
                        break;
                    case MediaType.Audio:
                        thumbnailPath = await ExtractAudioThumbnailAsync(mediaItem.FilePath, cachedThumbnail);
                        break;
                }

                cancellationToken.ThrowIfCancellationRequested();

                if (!string.IsNullOrEmpty(thumbnailPath) && File.Exists(thumbnailPath))
                {
                    mediaItem.ThumbnailPath = thumbnailPath;
                    return await LoadBitmapImageAsync(thumbnailPath);
                }

                return null;
            }
            catch (OperationCanceledException)
            {
                throw; // Propagate cancellation
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error generating thumbnail for {mediaItem.FileName}: {ex.Message}");
                return null;
            }
            finally
            {
                mediaItem.IsLoadingThumbnail = false;
            }
        }

        /// <summary>
        /// Extract thumbnail from video using LibVLCSharp
        /// </summary>
        private async Task<string> ExtractVideoThumbnailAsync(string videoPath, string outputPath)
        {
            try
            {
                using (var extractor = new LibVLCThumbnailExtractor())
                {
                    return await extractor.ExtractThumbnailAsync(videoPath, outputPath);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LibVLC thumbnail extraction error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Extract thumbnail from audio file (embedded art or folder image)
        /// </summary>
        private async Task<string> ExtractAudioThumbnailAsync(string audioPath, string outputPath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // First, try to find album art in the same folder
                    string folderPath = Path.GetDirectoryName(audioPath);
                    string folderAlbumArt = FindFolderAlbumArt(folderPath);

                    if (!string.IsNullOrEmpty(folderAlbumArt) && File.Exists(folderAlbumArt))
                    {
                        // Copy and resize the found album art
                        File.Copy(folderAlbumArt, outputPath, true);
                        return outputPath;
                    }

                    // Try to extract embedded album art using TagLib-Sharp
                    try
                    {
                        var file = TagLib.File.Create(audioPath);
                        if (file.Tag.Pictures.Length > 0)
                        {
                            var picture = file.Tag.Pictures[0];
                            File.WriteAllBytes(outputPath, picture.Data.Data);
                            return outputPath;
                        }
                    }
                    catch (Exception tagEx)
                    {
                        Debug.WriteLine($"TagLib extraction error: {tagEx.Message}");
                    }

                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Audio thumbnail extraction error: {ex.Message}");
                }

                return null;
            });
        }

        /// <summary>
        /// Find album art image in folder
        /// </summary>
        private string FindFolderAlbumArt(string folderPath)
        {
            try
            {
                // Check for common album art filenames
                foreach (string albumArtName in _commonAlbumArtNames)
                {
                    string albumArtPath = Path.Combine(folderPath, albumArtName);
                    if (File.Exists(albumArtPath))
                    {
                        return albumArtPath;
                    }
                }

                // If not found, look for any jpg or png in the folder
                string[] imageFiles = Directory.GetFiles(folderPath, "*.jpg")
                    .Concat(Directory.GetFiles(folderPath, "*.jpeg"))
                    .Concat(Directory.GetFiles(folderPath, "*.png"))
                    .ToArray();

                if (imageFiles.Length > 0)
                {
                    return imageFiles[0];
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error finding folder album art: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Get cached thumbnail path based on file hash
        /// </summary>
        private string GetCachedThumbnailPath(string filePath)
        {
            string fileHash = GetFileHash(filePath);
            return Path.Combine(_thumbnailCacheFolder, $"{fileHash}.jpg");
        }

        /// <summary>
        /// Generate hash for file path to use as cache key
        /// </summary>
        private string GetFileHash(string filePath)
        {
            using (var md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(filePath));
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }

        /// <summary>
        /// Load BitmapImage from file path asynchronously
        /// </summary>
        private async Task<BitmapImage> LoadBitmapImageAsync(string imagePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.DecodePixelWidth = 320; // Resize for performance
                    bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
                    bitmap.EndInit();
                    bitmap.Freeze(); // Make it cross-thread accessible
                    return bitmap;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error loading bitmap: {ex.Message}");
                    return null;
                }
            });
        }
    }
}
