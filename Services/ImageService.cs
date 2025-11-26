using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace MyPhotoBiz.Services
{
    public class ImageService : IImageService
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ImageService> _logger;

        public ImageService(IWebHostEnvironment env, ILogger<ImageService> logger)
        {
            _env = env;
            _logger = logger;
        }

    public async Task<string> ProcessAndSaveProfileImageAsync(IFormFile file, string userId)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));
            if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));

            var allowed = new[] { ".jpg", ".jpeg", ".png" };
            var maxBytes = 2 * 1024 * 1024; // 2 MB
            var fileExt = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(fileExt) || !allowed.Contains(fileExt))
            {
                throw new InvalidOperationException("Only JPG/JPEG/PNG images are allowed.");
            }
            if (file.Length > maxBytes)
            {
                throw new InvalidOperationException("Image must be smaller than 2 MB.");
            }

            var uploadsRoot = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "profiles");
            Directory.CreateDirectory(uploadsRoot);

            var baseFileName = userId;

            // Save avatar (256x256)
            var avatarFileName = baseFileName + ".jpg";
            var avatarPath = Path.Combine(uploadsRoot, avatarFileName);

            // Save thumbnail (64x64)
            var thumbFileName = baseFileName + "_thumb.jpg";
            var thumbPath = Path.Combine(uploadsRoot, thumbFileName);

            try
            {
                using (var inStream = file.OpenReadStream())
                using (var image = await Image.LoadAsync(inStream))
                {
                    // avatar
                    image.Mutate(x => x.Resize(new ResizeOptions { Size = new SixLabors.ImageSharp.Size(256, 256), Mode = ResizeMode.Crop }));
                    var encoder = new JpegEncoder { Quality = 85 };
                    await image.SaveAsJpegAsync(avatarPath, encoder);
                }

                // Re-open and create a separate thumbnail
                using (var inStream2 = file.OpenReadStream())
                using (var image2 = await Image.LoadAsync(inStream2))
                {
                    image2.Mutate(x => x.Resize(new ResizeOptions { Size = new SixLabors.ImageSharp.Size(64, 64), Mode = ResizeMode.Crop }));
                    var encoder = new JpegEncoder { Quality = 80 };
                    await image2.SaveAsJpegAsync(thumbPath, encoder);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process image for user {UserId}", userId);
                throw new InvalidOperationException("Unable to process the uploaded image.");
            }

            var publicUrl = $"/uploads/profiles/{avatarFileName}?v={DateTime.UtcNow.Ticks}";
            return publicUrl;
        }

        public async Task<(string filePath, string thumbnailPath, string publicUrl)> ProcessAndSaveAlbumImageAsync(IFormFile file, int albumId, string baseFileName)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));
            if (string.IsNullOrEmpty(baseFileName)) throw new ArgumentNullException(nameof(baseFileName));

            var allowed = new[] { ".jpg", ".jpeg", ".png" };
            var maxBytes = 20 * 1024 * 1024; // 20 MB
            var fileExt = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(fileExt) || !allowed.Contains(fileExt))
            {
                throw new InvalidOperationException("Only JPG/JPEG/PNG images are allowed.");
            }
            if (file.Length > maxBytes)
            {
                throw new InvalidOperationException("Image must be smaller than 20 MB.");
            }

            var uploadsRoot = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "albums", albumId.ToString());
            Directory.CreateDirectory(uploadsRoot);

            var avatarFileName = baseFileName + ".jpg"; // normalized
            var avatarPath = Path.Combine(uploadsRoot, avatarFileName);
            var thumbFileName = baseFileName + "_thumb.jpg";
            var thumbPath = Path.Combine(uploadsRoot, thumbFileName);

            try
            {
                using (var inStream = file.OpenReadStream())
                using (var image = await Image.LoadAsync(inStream))
                {
                    // Resize full image to max width 1920 (preserve ratio)
                    var size = image.Size;
                    if (size.Width > 1920)
                    {
                        image.Mutate(x => x.Resize(new ResizeOptions { Size = new SixLabors.ImageSharp.Size(1920, 0), Mode = ResizeMode.Max }));
                    }
                    var encoder = new JpegEncoder { Quality = 90 };
                    await image.SaveAsJpegAsync(avatarPath, encoder);
                }

                // thumbnail
                using (var inStream2 = file.OpenReadStream())
                using (var image2 = await Image.LoadAsync(inStream2))
                {
                    image2.Mutate(x => x.Resize(new ResizeOptions { Size = new SixLabors.ImageSharp.Size(400, 300), Mode = ResizeMode.Crop }));
                    var encoder = new JpegEncoder { Quality = 80 };
                    await image2.SaveAsJpegAsync(thumbPath, encoder);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process album image");
                throw new InvalidOperationException("Unable to process the uploaded image.");
            }

            var publicUrl = $"/uploads/albums/{albumId}/{avatarFileName}?v={DateTime.UtcNow.Ticks}";
            return (avatarPath, thumbPath, publicUrl);
        }
    }
}
