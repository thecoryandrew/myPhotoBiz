using MyPhotoBiz.Models;

namespace MyPhotoBiz.Services
{
    public interface IPhotoShootService
    {
        Task<IEnumerable<PhotoShoot>> GetAllPhotoShootsAsync();
        Task<PhotoShoot?> GetPhotoShootByIdAsync(int id);
        Task<IEnumerable<PhotoShoot>> GetUpcomingPhotoShootsAsync(int daysAhead = 7);
        Task<IEnumerable<PhotoShoot>> GetPhotoShootsByClientIdAsync(int clientId); // Added missing method
        Task<PhotoShoot> CreatePhotoShootAsync(PhotoShoot shoot);
        Task<PhotoShoot> UpdatePhotoShootAsync(PhotoShoot shoot);
        Task<bool> DeletePhotoShootAsync(int id);
        Task<int> GetPhotoShootsCountAsync();
        Task<int> GetPendingPhotoShootsCountAsync();
    }
}