using Microsoft.Extensions.Logging;
using MedicalLabAnalyzer.Models;
using System.Drawing;
using System.Drawing.Imaging;
using SkiaSharp;
using OpenCvSharp;
using FFMpegCore;
using FFMpegCore.Enums;
using System.IO;

namespace MedicalLabAnalyzer.Services
{
    public class MediaService
    {
        private readonly ILogger&lt;MediaService&gt; _logger;
        private readonly string _mediaBasePath;
        private readonly string _thumbnailPath;
        private readonly string _tempPath;
        
        // Supported file formats
        private readonly string[] _supportedImageFormats = { ".jpg", ".jpeg", ".png", ".bmp", ".tiff", ".gif" };
        private readonly string[] _supportedVideoFormats = { ".mp4", ".avi", ".mov", ".wmv", ".flv", ".mkv" };
        
        public MediaService(ILogger&lt;MediaService&gt; logger)
        {
            _logger = logger;
            _mediaBasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Media");
            _thumbnailPath = Path.Combine(_mediaBasePath, "Thumbnails");
            _tempPath = Path.Combine(_mediaBasePath, "Temp");
            
            InitializeDirectories();
        }
        
        private void InitializeDirectories()
        {
            try
            {
                Directory.CreateDirectory(_mediaBasePath);
                Directory.CreateDirectory(_thumbnailPath);
                Directory.CreateDirectory(_tempPath);
                Directory.CreateDirectory(Path.Combine(_mediaBasePath, "Patients"));
                Directory.CreateDirectory(Path.Combine(_mediaBasePath, "Exams"));
                Directory.CreateDirectory(Path.Combine(_mediaBasePath, "CASA"));
                Directory.CreateDirectory(Path.Combine(_mediaBasePath, "Medical_Images"));
                Directory.CreateDirectory(Path.Combine(_mediaBasePath, "Videos"));
                
                _logger.LogInformation("Media directories initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize media directories");
                throw;
            }
        }
        
        public async Task&lt;string?&gt; SavePatientImageAsync(byte[] imageData, int patientId, string originalFileName)
        {
            try
            {
                var patientDir = Path.Combine(_mediaBasePath, "Patients", patientId.ToString());
                Directory.CreateDirectory(patientDir);
                
                var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
                if (!_supportedImageFormats.Contains(extension))
                {
                    throw new ArgumentException($"Unsupported image format: {extension}");
                }
                
                var fileName = $"profile_{DateTime.Now:yyyyMMdd_HHmmss}{extension}";
                var filePath = Path.Combine(patientDir, fileName);
                
                // Resize and optimize image
                var optimizedImage = await OptimizeImageAsync(imageData, 800, 800, 85);
                await File.WriteAllBytesAsync(filePath, optimizedImage);
                
                // Generate thumbnail
                var thumbnailData = await CreateThumbnailAsync(optimizedImage, 150, 150);
                var thumbnailFileName = $"thumb_{fileName}";
                var thumbnailFilePath = Path.Combine(_thumbnailPath, thumbnailFileName);
                await File.WriteAllBytesAsync(thumbnailFilePath, thumbnailData);
                
                _logger.LogInformation($"Patient image saved: {filePath}");
                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to save patient image for patient {patientId}");
                return null;
            }
        }
        
        public async Task&lt;string?&gt; SaveExamImageAsync(byte[] imageData, int examId, string originalFileName, AttachmentCategory category = AttachmentCategory.MedicalImage)
        {
            try
            {
                var examDir = Path.Combine(_mediaBasePath, "Exams", examId.ToString());
                Directory.CreateDirectory(examDir);
                
                var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
                if (!_supportedImageFormats.Contains(extension))
                {
                    throw new ArgumentException($"Unsupported image format: {extension}");
                }
                
                var fileName = $"{category}_{DateTime.Now:yyyyMMdd_HHmmss}{extension}";
                var filePath = Path.Combine(examDir, fileName);
                
                // For medical images, preserve quality
                var optimizedImage = await OptimizeImageAsync(imageData, 1920, 1920, 95);
                await File.WriteAllBytesAsync(filePath, optimizedImage);
                
                // Generate thumbnail
                var thumbnailData = await CreateThumbnailAsync(optimizedImage, 200, 200);
                var thumbnailFileName = $"thumb_{fileName}";
                var thumbnailFilePath = Path.Combine(_thumbnailPath, thumbnailFileName);
                await File.WriteAllBytesAsync(thumbnailFilePath, thumbnailData);
                
                _logger.LogInformation($"Exam image saved: {filePath}");
                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to save exam image for exam {examId}");
                return null;
            }
        }
        
        public async Task&lt;string?&gt; SaveCasaVideoAsync(byte[] videoData, int examId, string originalFileName)
        {
            try
            {
                var casaDir = Path.Combine(_mediaBasePath, "CASA", examId.ToString());
                Directory.CreateDirectory(casaDir);
                
                var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
                if (!_supportedVideoFormats.Contains(extension))
                {
                    throw new ArgumentException($"Unsupported video format: {extension}");
                }
                
                var fileName = $"casa_video_{DateTime.Now:yyyyMMdd_HHmmss}{extension}";
                var filePath = Path.Combine(casaDir, fileName);
                
                // Save original video
                await File.WriteAllBytesAsync(filePath, videoData);
                
                // Generate video thumbnail
                var thumbnailData = await CreateVideoThumbnailAsync(filePath);
                if (thumbnailData != null)
                {
                    var thumbnailFileName = $"thumb_{Path.GetFileNameWithoutExtension(fileName)}.jpg";
                    var thumbnailFilePath = Path.Combine(_thumbnailPath, thumbnailFileName);
                    await File.WriteAllBytesAsync(thumbnailFilePath, thumbnailData);
                }
                
                // Optimize video for analysis (convert to MP4 if needed)
                var optimizedPath = await OptimizeVideoForAnalysisAsync(filePath);
                
                _logger.LogInformation($"CASA video saved: {filePath}");
                return optimizedPath ?? filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to save CASA video for exam {examId}");
                return null;
            }
        }
        
        public async Task&lt;byte[]&gt; OptimizeImageAsync(byte[] imageData, int maxWidth, int maxHeight, int quality = 85)
        {
            try
            {
                using var inputStream = new MemoryStream(imageData);
                using var original = SKBitmap.Decode(inputStream);
                
                if (original == null)
                    throw new ArgumentException("Invalid image data");
                
                // Calculate new dimensions while maintaining aspect ratio
                var (newWidth, newHeight) = CalculateNewDimensions(original.Width, original.Height, maxWidth, maxHeight);
                
                // Resize image
                using var resized = original.Resize(new SKImageInfo(newWidth, newHeight), SKFilterQuality.High);
                using var image = SKImage.FromBitmap(resized);
                using var data = image.Encode(SKEncodedImageFormat.Jpeg, quality);
                
                return data.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to optimize image");
                return imageData; // Return original if optimization fails
            }
        }
        
        public async Task&lt;byte[]&gt; CreateThumbnailAsync(byte[] imageData, int width, int height)
        {
            try
            {
                using var inputStream = new MemoryStream(imageData);
                using var original = SKBitmap.Decode(inputStream);
                
                if (original == null)
                    throw new ArgumentException("Invalid image data");
                
                // Create square thumbnail with cropping
                var size = Math.Min(original.Width, original.Height);
                var x = (original.Width - size) / 2;
                var y = (original.Height - size) / 2;
                
                using var cropped = new SKBitmap(size, size);
                using var canvas = new SKCanvas(cropped);
                
                var srcRect = new SKRect(x, y, x + size, y + size);
                var destRect = new SKRect(0, 0, size, size);
                canvas.DrawBitmap(original, srcRect, destRect);
                
                // Resize to thumbnail size
                using var thumbnail = cropped.Resize(new SKImageInfo(width, height), SKFilterQuality.High);
                using var image = SKImage.FromBitmap(thumbnail);
                using var data = image.Encode(SKEncodedImageFormat.Jpeg, 80);
                
                return data.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create thumbnail");
                return imageData;
            }
        }
        
        public async Task&lt;byte[]?&gt; CreateVideoThumbnailAsync(string videoPath)
        {
            try
            {
                var tempThumbnailPath = Path.Combine(_tempPath, $"thumb_{Guid.NewGuid()}.jpg");
                
                await FFMpegArguments
                    .FromFileInput(videoPath)
                    .OutputToFile(tempThumbnailPath, true, options =&gt; options
                        .WithVideoCodec(VideoCodec.LibX264)
                        .WithFrameOutputCount(1)
                        .Seek(TimeSpan.FromSeconds(1)))
                    .ProcessAsynchronously();
                
                if (File.Exists(tempThumbnailPath))
                {
                    var thumbnailData = await File.ReadAllBytesAsync(tempThumbnailPath);
                    File.Delete(tempThumbnailPath);
                    
                    // Optimize thumbnail
                    return await CreateThumbnailAsync(thumbnailData, 200, 150);
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to create video thumbnail for {videoPath}");
                return null;
            }
        }
        
        public async Task&lt;string?&gt; OptimizeVideoForAnalysisAsync(string inputPath)
        {
            try
            {
                if (!File.Exists(inputPath))
                    return null;
                
                var fileName = Path.GetFileNameWithoutExtension(inputPath);
                var outputPath = Path.Combine(Path.GetDirectoryName(inputPath)!, $"{fileName}_optimized.mp4");
                
                // Convert to MP4 with optimal settings for analysis
                await FFMpegArguments
                    .FromFileInput(inputPath)
                    .OutputToFile(outputPath, true, options =&gt; options
                        .WithVideoCodec(VideoCodec.LibX264)
                        .WithConstantRateFactor(23)
                        .WithVideoFilters(filterOptions =&gt; filterOptions
                            .Scale(1280, 720)) // Standardize resolution for AI analysis
                        .WithFramerate(30)
                        .WithAudioCodec(AudioCodec.Aac)
                        .WithAudioBitrate(128))
                    .ProcessAsynchronously();
                
                if (File.Exists(outputPath))
                {
                    _logger.LogInformation($"Video optimized for analysis: {outputPath}");
                    return outputPath;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to optimize video for analysis: {inputPath}");
                return null;
            }
        }
        
        public async Task&lt;VideoInfo?&gt; GetVideoInfoAsync(string videoPath)
        {
            try
            {
                var analysis = await FFProbe.AnalyseAsync(videoPath);
                return new VideoInfo
                {
                    Duration = analysis.Duration,
                    Width = analysis.PrimaryVideoStream?.Width ?? 0,
                    Height = analysis.PrimaryVideoStream?.Height ?? 0,
                    FrameRate = analysis.PrimaryVideoStream?.FrameRate ?? 0,
                    BitRate = analysis.PrimaryVideoStream?.BitRate ?? 0,
                    Codec = analysis.PrimaryVideoStream?.CodecName ?? "Unknown"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get video info for {videoPath}");
                return null;
            }
        }
        
        public async Task&lt;List&lt;byte[]&gt;&gt; ExtractVideoFramesAsync(string videoPath, int maxFrames = 100, TimeSpan? startTime = null, TimeSpan? endTime = null)
        {
            var frames = new List&lt;byte[]&gt;();
            
            try
            {
                using var capture = new VideoCapture(videoPath);
                if (!capture.IsOpened())
                {
                    throw new ArgumentException("Cannot open video file");
                }
                
                var totalFrames = (int)capture.FrameCount;
                var fps = capture.Fps;
                
                var startFrame = startTime.HasValue ? (int)(startTime.Value.TotalSeconds * fps) : 0;
                var endFrame = endTime.HasValue ? (int)(endTime.Value.TotalSeconds * fps) : totalFrames;
                
                var frameInterval = Math.Max(1, (endFrame - startFrame) / maxFrames);
                
                capture.PosFrames = startFrame;
                
                using var frame = new Mat();
                for (int i = startFrame; i &lt; endFrame && frames.Count &lt; maxFrames; i += frameInterval)
                {
                    capture.PosFrames = i;
                    if (capture.Read(frame) && !frame.Empty())
                    {
                        var frameData = frame.ToBytes(".jpg");
                        frames.Add(frameData);
                    }
                }
                
                _logger.LogInformation($"Extracted {frames.Count} frames from video {videoPath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to extract frames from video {videoPath}");
            }
            
            return frames;
        }
        
        public bool IsImageFile(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return _supportedImageFormats.Contains(extension);
        }
        
        public bool IsVideoFile(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return _supportedVideoFormats.Contains(extension);
        }
        
        public async Task&lt;bool&gt; DeleteFileAsync(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    // Also delete thumbnail if exists
                    var fileName = Path.GetFileName(filePath);
                    var thumbnailPath = Path.Combine(_thumbnailPath, $"thumb_{fileName}");
                    if (File.Exists(thumbnailPath))
                    {
                        File.Delete(thumbnailPath);
                    }
                    
                    File.Delete(filePath);
                    _logger.LogInformation($"File deleted: {filePath}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to delete file: {filePath}");
                return false;
            }
        }
        
        public async Task&lt;long&gt; GetDirectorySizeAsync(string directoryPath)
        {
            try
            {
                if (!Directory.Exists(directoryPath))
                    return 0;
                
                var directoryInfo = new DirectoryInfo(directoryPath);
                return directoryInfo.EnumerateFiles("*", SearchOption.AllDirectories).Sum(file =&gt; file.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to calculate directory size: {directoryPath}");
                return 0;
            }
        }
        
        public async Task CleanupTempFilesAsync()
        {
            try
            {
                if (Directory.Exists(_tempPath))
                {
                    var tempFiles = Directory.GetFiles(_tempPath);
                    var cutoffTime = DateTime.Now.AddHours(-1); // Delete files older than 1 hour
                    
                    foreach (var file in tempFiles)
                    {
                        var fileInfo = new FileInfo(file);
                        if (fileInfo.CreationTime &lt; cutoffTime)
                        {
                            File.Delete(file);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup temp files");
            }
        }
        
        private static (int width, int height) CalculateNewDimensions(int originalWidth, int originalHeight, int maxWidth, int maxHeight)
        {
            var aspectRatio = (double)originalWidth / originalHeight;
            
            int newWidth, newHeight;
            
            if (originalWidth &gt; originalHeight)
            {
                newWidth = Math.Min(maxWidth, originalWidth);
                newHeight = (int)(newWidth / aspectRatio);
                
                if (newHeight &gt; maxHeight)
                {
                    newHeight = maxHeight;
                    newWidth = (int)(newHeight * aspectRatio);
                }
            }
            else
            {
                newHeight = Math.Min(maxHeight, originalHeight);
                newWidth = (int)(newHeight * aspectRatio);
                
                if (newWidth &gt; maxWidth)
                {
                    newWidth = maxWidth;
                    newHeight = (int)(newWidth / aspectRatio);
                }
            }
            
            return (newWidth, newHeight);
        }
    }
    
    public class VideoInfo
    {
        public TimeSpan Duration { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public double FrameRate { get; set; }
        public long BitRate { get; set; }
        public string Codec { get; set; } = string.Empty;
    }
}