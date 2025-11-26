
// Services/IPhotoService.cs
using MyPhotoBiz.Models;

namespace MyPhotoBiz.Services
{
    public interface IPhotoService
    {
        Task<IEnumerable<Photo>> GetPhotosByAlbumIdAsync(int albumId);
        Task<Photo?> GetPhotoByIdAsync(int id);
        Task<Photo> CreatePhotoAsync(Photo photo);
        Task<bool> DeletePhotoAsync(int id);
        Task<Photo> UpdatePhotoAsync(Photo photo);
        Task<string> SavePhotoAsync(IFormFile file, string uploadsPath);
    }
}
