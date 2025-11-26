using MyPhotoBiz.Models;

namespace MyPhotoBiz.Services
{
    public interface IAlbumService
    {
        // Existing methods (you probably already have these)
        Task<Album?> GetAlbumByIdAsync(int id);
        Task<Album> CreateAlbumAsync(Album album);

        Task<Album> UpdateAlbumAsync(Album album);
        Task<bool> DeleteAlbumAsync(int id);

        // NEW METHODS - Add these if you don't have them
        Task<IEnumerable<Album>> GetAllAlbumsAsync();
        Task<IEnumerable<Album>> GetAlbumsByClientIdAsync(int clientId);
        Task<IEnumerable<Album>> GetAlbumsByPhotoShootIdAsync(int photoShootId);
        Task<Photo> UpdatePhotoAsync(Photo photo);
    }
}