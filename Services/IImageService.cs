using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace MyPhotoBiz.Services
{
    public interface IImageService
    {
        /// <summary>
        /// Process an uploaded profile image: validate, resize to multiple sizes, save to webroot and return the public URL (with cache-busting query param).
        /// </summary>
        Task<string> ProcessAndSaveProfileImageAsync(IFormFile file, string userId);

        /// <summary>
        /// Process and save an album image: validate, create full-size and thumbnail, save to album folder and return paths.
        /// </summary>
        Task<(string filePath, string thumbnailPath, string publicUrl)> ProcessAndSaveAlbumImageAsync(IFormFile file, int albumId, string baseFileName);
    }
}
