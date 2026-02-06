using LibVLCSharp.Shared;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Processing;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace CMVideo
{
    /// <summary>
    /// Extracts video thumbnails using LibVLCSharp video callbacks
    /// </summary>
    public class LibVLCThumbnailExtractor : IDisposable
    {
        private const uint Width = 320;
        private const uint Height = 180;
        private const uint BytePerPixel = 4; // RGBA format

        private static readonly uint Pitch;
        private static readonly uint Lines;

        static LibVLCThumbnailExtractor()
        {
            // Align dimensions to multiples of 32 for VLC performance
            Pitch = Align(Width * BytePerPixel);
            Lines = Align(Height);

            uint Align(uint size)
            {
                if (size % 32 == 0)
                    return size;
                return ((size / 32) + 1) * 32;
            }
        }

        private MemoryMappedFile _currentMappedFile;
        private MemoryMappedViewAccessor _currentMappedViewAccessor;
        private bool _frameCaptured;
        private string _outputPath;
        private readonly object _lockObject = new object();

        /// <summary>
        /// Extract thumbnail from video file
        /// </summary>
        public async Task<string> ExtractThumbnailAsync(string videoPath, string outputPath)
        {
            if (string.IsNullOrEmpty(videoPath) || !File.Exists(videoPath))
            {
                Debug.WriteLine($"Video file not found: {videoPath}");
                return null;
            }

            _outputPath = outputPath;
            _frameCaptured = false;

            try
            {
                // Initialize LibVLC core if not already done

                using (var libvlc = new LibVLC("--no-audio", "--no-spu"))
                using (var mediaPlayer = new MediaPlayer(libvlc))
                {
                    var cancellationTokenSource = new CancellationTokenSource();
                    var thumbnailTask = Task.CompletedTask;

                    // Set up event handlers
                    mediaPlayer.Playing += (s, e) =>
                    {
                        Debug.WriteLine("MediaPlayer started playing");
                    };

                    mediaPlayer.EncounteredError += (s, e) =>
                    {
                        Debug.WriteLine("MediaPlayer encountered error");
                        cancellationTokenSource.Cancel();
                    };

                    // Create media
                    using (var media = new Media(libvlc, new Uri(videoPath)))
                    {
                        // Configure video format and callbacks
                        mediaPlayer.SetVideoFormat("RV32", Width, Height, Pitch);
                        mediaPlayer.SetVideoCallbacks(Lock, null, Display);

                        // Start playback
                        mediaPlayer.Play(media);

                        // Wait for frame capture with timeout
                        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10), cancellationTokenSource.Token);
                        var completedTask = await Task.WhenAny(
                            Task.Run(() => WaitForFrameCapture(cancellationTokenSource.Token)),
                            timeoutTask
                        );

                        // Stop playback
                        mediaPlayer.Stop();

                        if (completedTask == timeoutTask)
                        {
                            Debug.WriteLine("Thumbnail extraction timed out");
                            return null;
                        }

                        // Check if frame was captured successfully
                        if (_frameCaptured && File.Exists(outputPath))
                        {
                            Debug.WriteLine($"Thumbnail extracted successfully: {outputPath}");
                            return outputPath;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error extracting thumbnail: {ex.Message}");
            }
            finally
            {
                // Clean up any remaining resources
                CleanupCurrentFrame();
            }

            return null;
        }

        /// <summary>
        /// Wait for frame to be captured
        /// </summary>
        private void WaitForFrameCapture(CancellationToken cancellationToken)
        {
            var timeout = TimeSpan.FromSeconds(10);
            var startTime = DateTime.Now;

            while (!_frameCaptured && !cancellationToken.IsCancellationRequested)
            {
                if (DateTime.Now - startTime > timeout)
                {
                    Debug.WriteLine("Frame capture timeout");
                    break;
                }

                Thread.Sleep(100);
            }
        }

        /// <summary>
        /// Lock callback - VLC calls this when it needs a buffer for video frame
        /// </summary>
        private IntPtr Lock(IntPtr opaque, IntPtr planes)
        {
            lock (_lockObject)
            {
                try
                {
                    _currentMappedFile = MemoryMappedFile.CreateNew(null, Pitch * Lines);
                    _currentMappedViewAccessor = _currentMappedFile.CreateViewAccessor();
                    Marshal.WriteIntPtr(planes, _currentMappedViewAccessor.SafeMemoryMappedViewHandle.DangerousGetHandle());
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Lock error: {ex.Message}");
                }

                return IntPtr.Zero;
            }
        }

        /// <summary>
        /// Display callback - VLC calls this when frame is ready to display
        /// We capture only the FIRST frame for thumbnail
        /// </summary>
        private void Display(IntPtr opaque, IntPtr picture)
        {
            lock (_lockObject)
            {
                if (_frameCaptured)
                {
                    // Already captured, just clean up
                    CleanupCurrentFrame();
                    return;
                }

                try
                {
                    if (_currentMappedFile != null && _currentMappedViewAccessor != null)
                    {
                        // Process and save the frame
                        ProcessAndSaveFrame();
                        _frameCaptured = true;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Display error: {ex.Message}");
                    CleanupCurrentFrame();
                }
            }
        }

        /// <summary>
        /// Process the captured frame and save as JPEG
        /// </summary>
        private void ProcessAndSaveFrame()
        {
            try
            {
                using (var image = new Image<SixLabors.ImageSharp.PixelFormats.Bgra32>((int)(Pitch / BytePerPixel), (int)Lines))
                using (var sourceStream = _currentMappedFile.CreateViewStream())
                {
                    // Read pixel data from memory-mapped file
                    var pixelMemoryGroup = image.GetPixelMemoryGroup();
                    for (int i = 0; i < pixelMemoryGroup.Count; i++)
                    {
                        sourceStream.Read(MemoryMarshal.AsBytes(pixelMemoryGroup[i].Span));
                    }

                    // Crop to actual dimensions (remove alignment padding)
                    image.Mutate(ctx => ctx.Crop((int)Width, (int)Height));

                    // Save as JPEG
                    using (var outputFile = File.Open(_outputPath, FileMode.Create))
                    {
                        image.SaveAsJpeg(outputFile, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder { Quality = 85 });
                    }

                    Debug.WriteLine($"Frame saved to: {_outputPath}");
                }
            }
            finally
            {
                CleanupCurrentFrame();
            }
        }

        /// <summary>
        /// Clean up current frame resources
        /// </summary>
        private void CleanupCurrentFrame()
        {
            try
            {
                _currentMappedViewAccessor?.Dispose();
                _currentMappedFile?.Dispose();
                _currentMappedViewAccessor = null;
                _currentMappedFile = null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Cleanup error: {ex.Message}");
            }
        }

        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            CleanupCurrentFrame();
        }
    }
}
