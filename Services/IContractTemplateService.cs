using MyPhotoBiz.Enums;
using MyPhotoBiz.Models;

namespace MyPhotoBiz.Services
{
    public interface IContractTemplateService
    {
        Task<IEnumerable<ContractTemplate>> GetAllTemplatesAsync();
        Task<IEnumerable<ContractTemplate>> GetActiveTemplatesAsync();
        Task<IEnumerable<ContractTemplate>> GetTemplatesByCategoryAsync(ContractTemplateCategory category);
        Task<ContractTemplate?> GetTemplateByIdAsync(int id);
        Task<ContractTemplate> CreateTemplateAsync(ContractTemplate template);
        Task<ContractTemplate> UpdateTemplateAsync(ContractTemplate template);
        Task<bool> DeleteTemplateAsync(int id);
        Task<bool> ToggleTemplateStatusAsync(int id);
        Task<ContractTemplate> DuplicateTemplateAsync(int id, string newName);
        Task<string> PopulateTemplateWithClientDataAsync(string templateContent, int clientProfileId);
    }
}
