using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyPhotoBiz.Data;
using MyPhotoBiz.Enums;
using MyPhotoBiz.Models;

namespace MyPhotoBiz.Services
{
    public class ContractTemplateService : IContractTemplateService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ContractTemplateService> _logger;

        public ContractTemplateService(ApplicationDbContext context, ILogger<ContractTemplateService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<ContractTemplate>> GetAllTemplatesAsync()
        {
            try
            {
                _logger.LogInformation("Retrieving all contract templates");
                return await _context.ContractTemplates
                    .AsNoTracking()
                    .OrderBy(ct => ct.Category)
                    .ThenBy(ct => ct.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all contract templates");
                throw;
            }
        }

        public async Task<IEnumerable<ContractTemplate>> GetActiveTemplatesAsync()
        {
            try
            {
                _logger.LogInformation("Retrieving active contract templates");
                return await _context.ContractTemplates
                    .AsNoTracking()
                    .Where(ct => ct.IsActive)
                    .OrderBy(ct => ct.Category)
                    .ThenBy(ct => ct.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active contract templates");
                throw;
            }
        }

        public async Task<IEnumerable<ContractTemplate>> GetTemplatesByCategoryAsync(ContractTemplateCategory category)
        {
            try
            {
                _logger.LogInformation("Retrieving templates for category: {Category}", category);
                return await _context.ContractTemplates
                    .AsNoTracking()
                    .Where(ct => ct.Category == category && ct.IsActive)
                    .OrderBy(ct => ct.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving templates for category: {Category}", category);
                throw;
            }
        }

        public async Task<ContractTemplate?> GetTemplateByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("Retrieving template with ID: {TemplateId}", id);
                return await _context.ContractTemplates
                    .AsNoTracking()
                    .FirstOrDefaultAsync(ct => ct.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving template with ID: {TemplateId}", id);
                throw;
            }
        }

        public async Task<ContractTemplate> CreateTemplateAsync(ContractTemplate template)
        {
            try
            {
                _logger.LogInformation("Creating new contract template: {TemplateName}", template.Name);
                template.CreatedDate = DateTime.UtcNow;
                _context.ContractTemplates.Add(template);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Template created successfully with ID: {TemplateId}", template.Id);
                return template;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating contract template: {TemplateName}", template.Name);
                throw;
            }
        }

        public async Task<ContractTemplate> UpdateTemplateAsync(ContractTemplate template)
        {
            try
            {
                _logger.LogInformation("Updating contract template with ID: {TemplateId}", template.Id);
                template.UpdatedDate = DateTime.UtcNow;
                _context.ContractTemplates.Update(template);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Template updated successfully: {TemplateId}", template.Id);
                return template;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating contract template with ID: {TemplateId}", template.Id);
                throw;
            }
        }

        public async Task<bool> DeleteTemplateAsync(int id)
        {
            try
            {
                _logger.LogInformation("Deleting contract template with ID: {TemplateId}", id);
                var template = await _context.ContractTemplates.FindAsync(id);
                if (template == null)
                {
                    _logger.LogWarning("Template with ID {TemplateId} not found", id);
                    return false;
                }

                _context.ContractTemplates.Remove(template);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Template deleted successfully: {TemplateId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting contract template with ID: {TemplateId}", id);
                throw;
            }
        }

        public async Task<bool> ToggleTemplateStatusAsync(int id)
        {
            try
            {
                _logger.LogInformation("Toggling status for template with ID: {TemplateId}", id);
                var template = await _context.ContractTemplates.FindAsync(id);
                if (template == null)
                {
                    _logger.LogWarning("Template with ID {TemplateId} not found", id);
                    return false;
                }

                template.IsActive = !template.IsActive;
                template.UpdatedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Template status toggled to {Status}: {TemplateId}", template.IsActive, id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling status for template with ID: {TemplateId}", id);
                throw;
            }
        }

        public async Task<ContractTemplate> DuplicateTemplateAsync(int id, string newName)
        {
            try
            {
                _logger.LogInformation("Duplicating template with ID: {TemplateId}", id);
                var originalTemplate = await _context.ContractTemplates
                    .AsNoTracking()
                    .FirstOrDefaultAsync(ct => ct.Id == id);

                if (originalTemplate == null)
                {
                    throw new InvalidOperationException($"Template with ID {id} not found");
                }

                var duplicatedTemplate = new ContractTemplate
                {
                    Name = newName,
                    Description = originalTemplate.Description,
                    Content = originalTemplate.Content,
                    Category = originalTemplate.Category,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = originalTemplate.CreatedBy
                };

                _context.ContractTemplates.Add(duplicatedTemplate);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Template duplicated successfully with new ID: {NewTemplateId}", duplicatedTemplate.Id);
                return duplicatedTemplate;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error duplicating template with ID: {TemplateId}", id);
                throw;
            }
        }

        public async Task<string> PopulateTemplateWithClientDataAsync(string templateContent, int clientProfileId)
        {
            try
            {
                _logger.LogInformation("Populating template with client data for ClientProfile ID: {ClientProfileId}", clientProfileId);

                var clientProfile = await _context.ClientProfiles
                    .Include(cp => cp.User)
                    .FirstOrDefaultAsync(cp => cp.Id == clientProfileId);

                if (clientProfile == null)
                {
                    _logger.LogWarning("ClientProfile with ID {ClientProfileId} not found", clientProfileId);
                    return templateContent;
                }

                // Replace placeholders with actual client data
                var populatedContent = templateContent
                    .Replace("{ClientFirstName}", clientProfile.User?.FirstName ?? "")
                    .Replace("{ClientLastName}", clientProfile.User?.LastName ?? "")
                    .Replace("{ClientFullName}", $"{clientProfile.User?.FirstName} {clientProfile.User?.LastName}".Trim())
                    .Replace("{ClientEmail}", clientProfile.User?.Email ?? "")
                    .Replace("{ClientPhone}", clientProfile.PhoneNumber ?? "")
                    .Replace("{ClientAddress}", clientProfile.Address ?? "")
                    .Replace("{CurrentDate}", DateTime.Now.ToString("MMMM dd, yyyy"))
                    .Replace("{CurrentYear}", DateTime.Now.Year.ToString());

                _logger.LogInformation("Template populated successfully for ClientProfile ID: {ClientProfileId}", clientProfileId);
                return populatedContent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error populating template with client data for ClientProfile ID: {ClientProfileId}", clientProfileId);
                throw;
            }
        }
    }
}
