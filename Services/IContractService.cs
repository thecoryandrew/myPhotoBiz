using MyPhotoBiz.Models;
using MyPhotoBiz.ViewModels;

namespace MyPhotoBiz.Services
{
    public interface IContractService
    {
        Task<List<Contract>> GetAllContractsAsync();
        Task<Contract?> GetContractByIdAsync(int id);
        Task<Contract> CreateContractAsync(CreateContractViewModel model);
        Task<Contract> UpdateContractAsync(EditContractViewModel model);
        Task<bool> DeleteContractAsync(int id);
        Task<string?> SignContractAsync(int id, string signatureBase64);
        Task<List<ClientSelectionViewModel>> GetClientsAsync();
        Task<List<PhotoShootSelectionViewModel>> GetPhotoShootsAsync();
        Task<List<BadgeSelectionViewModel>> GetBadgesAsync();
    }
}
