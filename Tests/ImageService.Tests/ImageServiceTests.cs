using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using MyPhotoBiz.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace ImageService.Tests
{
    public class ImageServiceTests
    {
        [Fact]
        public async Task ProcessAndSaveProfileImageAsync_CreatesFiles_ReturnsUrl()
        {
            // Arrange
            var tempRoot = Path.Combine(Path.GetTempPath(), "myphotobiz_test_webroot");
            if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, true);
            Directory.CreateDirectory(tempRoot);

            var env = new TestWebHostEnvironment { WebRootPath = tempRoot };
            var logger = new NullLogger<ImageService>();
            var service = new ImageService(env, logger);

            // Create a tiny image in memory
            using var ms = new MemoryStream();
            using (var img = new Image<Rgba32>(100, 100))
            {
                img.SaveAsPng(ms);
            }
            ms.Position = 0;
            IFormFile file = new FormFile(ms, 0, ms.Length, "file", "avatar.png") { Headers = new HeaderDictionary(), ContentType = "image/png" };

            // Act
            var url = await service.ProcessAndSaveProfileImageAsync(file, "testuser");

            // Assert
            Assert.NotNull(url);
            Assert.Contains("/uploads/profiles/testuser.jpg", url);
            var avatarPath = Path.Combine(tempRoot, "uploads", "profiles", "testuser.jpg");
            var thumbPath = Path.Combine(tempRoot, "uploads", "profiles", "testuser_thumb.jpg");
            Assert.True(File.Exists(avatarPath));
            Assert.True(File.Exists(thumbPath));

            // Cleanup
            Directory.Delete(tempRoot, true);
        }

        private class TestWebHostEnvironment : IWebHostEnvironment
        {
            public string WebRootPath { get; set; }
            public string EnvironmentName { get; set; }
            public string ApplicationName { get; set; }
            public string ContentRootPath { get; set; }
            public IFileProvider WebRootFileProvider { get; set; }
            public IFileProvider ContentRootFileProvider { get; set; }
        }
    }
}
