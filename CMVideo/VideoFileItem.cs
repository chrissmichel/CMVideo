using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Media.Imaging;

namespace CMVideo
{
    public enum MediaType
    {
        Video,
        Audio,
        Unknown
    }

    public class VideoFileItem : INotifyPropertyChanged
    {
        private string _fileName;
        private string _filePath;
        private string _fileSize;
        private string _duration;
        private BitmapImage _thumbnail;
        private string _thumbnailPath;
        private MediaType _mediaType;
        private bool _isLoadingThumbnail;

        public string FileName
        {
            get => _fileName;
            set
            {
                _fileName = value;
                OnPropertyChanged(nameof(FileName));
            }
        }

        public string FilePath
        {
            get => _filePath;
            set
            {
                _filePath = value;
                OnPropertyChanged(nameof(FilePath));
            }
        }

        public string FileSize
        {
            get => _fileSize;
            set
            {
                _fileSize = value;
                OnPropertyChanged(nameof(FileSize));
            }
        }

        public string Duration
        {
            get => _duration;
            set
            {
                _duration = value;
                OnPropertyChanged(nameof(Duration));
            }
        }

        public BitmapImage Thumbnail
        {
            get => _thumbnail;
            set
            {
                _thumbnail = value;
                OnPropertyChanged(nameof(Thumbnail));
            }
        }

        public string ThumbnailPath
        {
            get => _thumbnailPath;
            set
            {
                _thumbnailPath = value;
                OnPropertyChanged(nameof(ThumbnailPath));
            }
        }

        public MediaType MediaType
        {
            get => _mediaType;
            set
            {
                _mediaType = value;
                OnPropertyChanged(nameof(MediaType));
            }
        }

        public bool IsLoadingThumbnail
        {
            get => _isLoadingThumbnail;
            set
            {
                _isLoadingThumbnail = value;
                OnPropertyChanged(nameof(IsLoadingThumbnail));
            }
        }

        public VideoFileItem(string filePath)
        {
            FilePath = filePath;
            FileName = Path.GetFileName(filePath);
            IsLoadingThumbnail = false;

            // Determine media type based on extension
            string extension = Path.GetExtension(filePath).ToLower();
            string[] videoExtensions = { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".mpg", ".mpeg", ".3gp", ".ogv", ".ts", ".m2ts" };
            string[] audioExtensions = { ".mp3", ".wav", ".flac", ".m4a", ".aac", ".ogg", ".wma", ".opus" };

            if (Array.IndexOf(videoExtensions, extension) >= 0)
            {
                MediaType = MediaType.Video;
            }
            else if (Array.IndexOf(audioExtensions, extension) >= 0)
            {
                MediaType = MediaType.Audio;
            }
            else
            {
                MediaType = MediaType.Unknown;
            }

            try
            {
                FileInfo fileInfo = new FileInfo(filePath);
                if (fileInfo.Exists)
                {
                    double fileSizeInMB = fileInfo.Length / (1024.0 * 1024.0);
                    FileSize = $"{fileSizeInMB:F2} MB";
                }
                else
                {
                    FileSize = "N/A";
                }
            }
            catch
            {
                FileSize = "N/A";
            }

            Duration = ""; // Can be populated later if needed
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
