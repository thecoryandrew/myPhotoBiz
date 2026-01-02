
using MyPhotoBiz.Models;

namespace MyPhotoBiz.Services
{
    public interface IClientService
    {
        Task<IEnumerable<ClientProfile>> GetAllClientsAsync();
        Task<ClientProfile?> GetClientByIdAsync(int id);
        Task<ClientProfile?> GetClientByUserIdAsync(string userId);
        Task<ClientProfile> CreateClientAsync(ClientProfile clientProfile);
        Task<ClientProfile> UpdateClientAsync(ClientProfile clientProfile);
        Task<bool> DeleteClientAsync(int id);
        Task<IEnumerable<ClientProfile>> SearchClientsAsync(string searchTerm);
        Task<IEnumerable<GalleryAccess>> GetClientGalleryAccessesAsync(int clientProfileId);
    }
}
