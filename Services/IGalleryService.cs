using MyPhotoBiz.Models;
using MyPhotoBiz.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyPhotoBiz.Services
{
    public interface IGalleryService
    {
        // CRUD Operations
        Task<IEnumerable<GalleryListItemViewModel>> GetAllGalleriesAsync();
        Task<GalleryDetailsViewModel?> GetGalleryDetailsAsync(int id);
        Task<Gallery?> GetGalleryByIdAsync(int id);
        Task<Gallery> CreateGalleryAsync(CreateGalleryViewModel model);
        Task<Gallery> UpdateGalleryAsync(EditGalleryViewModel model);
        Task<bool> DeleteGalleryAsync(int id);
        Task<bool> ToggleGalleryStatusAsync(int id, bool isActive);

        // Access Management (Identity-based)
        Task<GalleryAccess> GrantAccessAsync(int galleryId, int clientProfileId, DateTime? expiryDate = null);
        Task<bool> RevokeAccessAsync(int galleryId, int clientProfileId);
        Task<bool> ValidateUserAccessAsync(int galleryId, string userId);
        Task<IEnumerable<GalleryAccess>> GetGalleryAccessesAsync(int galleryId);

        // Album Management
        Task<bool> AddAlbumsToGalleryAsync(int galleryId, List<int> albumIds);
        Task<bool> RemoveAlbumsFromGalleryAsync(int galleryId, List<int> albumIds);
        Task<List<AlbumSelectionViewModel>> GetAvailableAlbumsAsync(int? currentGalleryId = null);

        // Session Management
        Task<IEnumerable<GallerySessionViewModel>> GetGallerySessionsAsync(int galleryId);
        Task<bool> EndSessionAsync(int sessionId);
        Task<bool> EndAllSessionsAsync(int galleryId);

        // Analytics
        Task<GalleryStatsSummaryViewModel> GetGalleryStatsAsync();
        Task<string> GetGalleryAccessUrlAsync(int galleryId, string baseUrl);
    }
}
