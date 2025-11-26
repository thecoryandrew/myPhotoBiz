
// Fixed Services/IClientService.cs
using MyPhotoBiz.Models;

namespace MyPhotoBiz.Services
{
    public interface IClientService
    {
        Task<IEnumerable<Client>> GetAllClientsAsync();
        Task<Client?> GetClientByIdAsync(int id);
        Task<Client?> GetClientByUserIdAsync(string userId); // Added missing method
        Task<Client> CreateClientAsync(Client client);
        Task<Client> UpdateClientAsync(Client client);
        Task<bool> DeleteClientAsync(int id);
        Task<IEnumerable<Client>> SearchClientsAsync(string searchTerm);
    }
}